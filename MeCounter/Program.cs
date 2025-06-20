using MeCounter.BotController;
using MeCounter.Commands;
using MeCounter.Commands.Admin_commands;
using MeCounter.DataAccess.Postgres;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using MeCounter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MeCounter
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File("logs/meCounter.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Начало работы... (Версия: {Version})", version);

                var host = CreateHostBuilder(args).Build();

                using var scope = host.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                await EnsureDatabaseConnectionAsync(db, logger, host.Services.GetRequiredService<IConfiguration>());
                await db.Database.MigrateAsync();

                logger.LogInformation("Безмозглый Антон проснуля!");

                var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var tgController = scope.ServiceProvider.GetRequiredService<TgBotController>();
                var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };

                bot.StartReceiving(
                    tgController.HandleUpdateAsync,
                    tgController.HandleErrorAsync,
                    receiverOptions,
                    CancellationToken.None);

                var me = await bot.GetMe();
                logger.LogInformation("Бот запущен: @{Username} (ID: {Id}) (Версия: {Version})", me.Username, me.Id, version);

                await host.RunAsync();
                Log.Information("Конец работы.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Приложение завершилось с ошибкой. Версия: {Version}", version);
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddConfigurations(context.Configuration);
                    services.AddDatabase(context.Configuration);
                    services.AddRepositories();
                    services.AddServices();
                    services.AddCommandHandlers();
                    services.AddAdminCommandHandlers();
                    services.AddSerilog();
                    services.AddBotServices();
                });

        private static async Task EnsureDatabaseConnectionAsync(AppDbContext db, ILogger<Program> logger, IConfiguration config)
        {
            var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogWarning("Обнаружены непримененные миграции: {PendingMigrations}. Рекомендуется выполнить 'dotnet ef database update'.",
                    string.Join(", ", pendingMigrations));
            }

            var maxRetries = config.GetValue<int>("Database:MaxRetries", 5);
            var delaySeconds = config.GetValue<int>("Database:RetryDelaySeconds", 5);

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                if (await db.Database.CanConnectAsync())
                {
                    logger.LogInformation("Подключение к БД успешно.");
                    return;
                }
                logger.LogWarning("Попытка {Retry}/{MaxRetries}: подключение не удалось. Ждем {Delay} сек...",
                    retry, maxRetries, delaySeconds);
                if (retry == maxRetries)
                {
                    logger.LogError("Не удалось подключиться к БД после {MaxRetries} попыток.", maxRetries);
                    throw new Exception("Не удалось подключиться к БД.");
                }
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);
            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            services.AddSingleton<IAppDbContextFactory, AppDbContextFactory>();
            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<UsersRepository>();
            services.AddScoped<ChatsRepository>();
            services.AddScoped<PornRepository>();
            services.AddScoped<VideoMetadataRepository>();
            services.AddScoped<DiceStateRepository>();
            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<ITextProcessor, TextProcessor>();
            return services;
        }

        public static IServiceCollection AddCommandHandlers(this IServiceCollection services)
        {
            services.AddScoped<ICommandHandler, SchetchikCommandHandler>();
            services.AddScoped<ICommandHandler, NoSchetchikCommandHandler>();
            services.AddScoped<ICommandHandler, PornCommandHandler>();
            services.AddScoped<ICommandHandler, VersionCommandHandler>();
            services.AddScoped<ICommandHandler, SaveVideoCommandHandler>();
            services.AddScoped<ICommandHandler, SendVideoCommandHandler>();
            services.AddScoped<ICommandHandler, AdminCommandHandler>();
            return services;
        }

        public static IServiceCollection AddAdminCommandHandlers(this IServiceCollection services)
        {
            services.AddScoped<SetCounterCommandHandler>();
            services.AddScoped<SetAdminCommandHandler>();
            services.AddScoped<SetCountedCommandHandler>();
            services.AddScoped<RestoreLinksCommandHandler>();
            return services;
        }
        public static IServiceCollection AddBotServices(this IServiceCollection services)
        {
            services.AddSingleton<ITelegramBotClient>(sp =>
                new TelegramBotClient(sp.GetRequiredService<IConfiguration>()["Telegram:BotToken"]
                    ?? throw new InvalidOperationException("Токен не найден")));
            services.AddScoped<TgBotController>();
            return services;
        }
    }
}