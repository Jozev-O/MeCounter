using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MeCounter.DataAccess.Postgres
{
    public interface IAppDbContextFactory
    {
        AppDbContext Create();
    }

    public class AppDbContextFactory : IAppDbContextFactory, IDesignTimeDbContextFactory<AppDbContext>
    {
        
        public AppDbContextFactory()
        {
        }

        public AppDbContext Create()
        {
            return CreateDbContext(null);
        }

        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=passw0rd;Database=telegrambot_db");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}