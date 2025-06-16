using MeCounter.Commands;
using MeCounter.Commands.Admin_commands;
using MeCounter.DataAccess.Postgres;
using MeCounter.DataAccess.Postgres.Repositories;
using MeCounter.Interfaces;
using MeCounter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MeCounter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Настройка Serilog
            Log.Logger = new LoggerConfiguration()
                   .MinimumLevel.Information()
                   .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                   .Enrich.FromLogContext()
                   .WriteTo.Console(
                       outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
                       theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                   .WriteTo.File("logs/meCounter.log", rollingInterval: RollingInterval.Day)
                   .CreateLogger();
            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            try
            {
                Log.Information("Начало работы... (Версия: {version})", version);

                // Настройка конфигурации
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();

                var services = new ServiceCollection();

                // Настройка сервисов и их зависимостей
                ConfigureServices(services, configuration);

                var serviceProvider = services.BuildServiceProvider();

                // Проверка подключения к БД
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                await EnsureDatabaseConnectionAsync(db, logger);

                await db.Database.MigrateAsync();

                logger.LogInformation("Безмозглый Антон проснуля!");

                // Инициализация Telegram бота
                var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var tgController = scope.ServiceProvider.GetRequiredService<TgBotController>();

                // Опции приёма обновлений
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = []
                };

                // Запуск бота
                var cts = new CancellationTokenSource();
                bot.StartReceiving(
                    tgController.HandleUpdateAsync,
                    tgController.HandleErrorAsync,
                    receiverOptions,
                    cts.Token);
                var me = await bot.GetMe();
                logger.LogInformation("Бот запущен: @{Username} (ID: {Id}) (Версия: {version})", me.Username, me.Id, version);

                // Удержание приложения до сигнала завершения
                await Task.Delay(Timeout.Infinite, cts.Token);
                Log.Information("Конец работы.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Приложение завершилось с ошибкой.");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Регистрация логгинга
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            services.AddSingleton(configuration);

            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(
                configuration["Telegram:BotToken"] ?? throw new InvalidOperationException("Токен не найден")));

            // База данных
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Репозитории
            services.AddScoped<UsersRepository>();
            services.AddScoped<ChatsRepository>();
            services.AddScoped<PornRepository>();
            services.AddScoped<VideoMetadataRepository>();

            // Сервисы обработки
            services.AddScoped<TextProcessor>();

            // Обработчики подкоманд администратора
            services.AddScoped(sp =>
                new SetCounterCommandHandler(
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped(sp =>
                new SetAdminCommandHandler(
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped(sp =>
                new SetCountedCommandHandler(
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped(sp =>
                new RestoreLinksCommandHandler(
                    sp.GetRequiredService<ChatsRepository>(),
                    sp.GetRequiredService<UsersRepository>(),
                    sp.GetRequiredService<AppDbContext>()));

            // Основные обработчики команд
            services.AddScoped(sp =>
                new SchetchikCommandHandler(
                    sp.GetRequiredService<ChatsRepository>(),
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped(sp =>
                new NoSchetchikCommandHandler(
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped<VersionCommandHandler>();

            services.AddScoped(sp =>
                new PornCommandHandler(
                    sp.GetRequiredService<ILogger<PornCommandHandler>>(),
                    sp.GetRequiredService<PornRepository>(),
                    sp.GetRequiredService<UsersRepository>()));

            services.AddScoped(sp =>
                new SaveVideoCommandHandler(
                    sp.GetRequiredService<ILogger<SaveVideoCommandHandler>>(),
                    sp.GetRequiredService<VideoMetadataRepository>(),
                    sp.GetRequiredService<ITelegramBotClient>()));

            services.AddScoped(sp =>
                new SendVideoCommandHandler(
                    sp.GetRequiredService<ILogger<SendVideoCommandHandler>>(),
                    sp.GetRequiredService<VideoMetadataRepository>(),
                    sp.GetRequiredService<ITelegramBotClient>()));

            // Админский обработчик с явными зависимостями
            services.AddScoped(sp => new AdminCommandHandler(
                sp.GetRequiredService<ILogger<AdminCommandHandler>>(),
                sp.GetRequiredService<UsersRepository>(),
                sp.GetRequiredService<SetCounterCommandHandler>(),
                sp.GetRequiredService<SetAdminCommandHandler>(),
                sp.GetRequiredService<SetCountedCommandHandler>(),
                sp.GetRequiredService<RestoreLinksCommandHandler>()
            ));

            // Регистрация интерфейсов команд
            services.AddScoped<ICommandHandler, SaveVideoCommandHandler>();
            services.AddScoped<ICommandHandler, SendVideoCommandHandler>();
            services.AddScoped<ICommandHandler, VersionCommandHandler>();
            services.AddScoped<ICommandHandler, AdminCommandHandler>();
            services.AddScoped<ICommandHandler, SchetchikCommandHandler>();
            services.AddScoped<ICommandHandler, NoSchetchikCommandHandler>();
            services.AddScoped<ICommandHandler, PornCommandHandler>();


            // Основной контроллер бота
            services.AddScoped<TgBotController>();
        }

        private static async Task EnsureDatabaseConnectionAsync(AppDbContext db, ILogger<Program> logger)
        {
            const int maxRetries = 5;
            const int delaySeconds = 5;

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                if (await db.Database.CanConnectAsync())
                {
                    logger.LogInformation("Подключение к БД успешно.");
                    return;
                }
                logger.LogWarning("Попытка {Retry}/{MaxRetries}: подключение не удалось. Ждем {Delay} сек...", retry, maxRetries, delaySeconds);
                if (retry == maxRetries)
                {
                    logger.LogError("Не удалось подключиться к БД после {MaxRetries} попыток.", maxRetries);
                    throw new Exception("Не удалось подключиться к БД.");
                }
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}