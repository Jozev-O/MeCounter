using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeCounter.DataAccess.Postgres;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

         //👉 ЗАМЕНИ СТРОКУ ПОДКЛЮЧЕНИЯ НА СВОЮ
        //optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=passw0rd;Database=telegrambot_db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
