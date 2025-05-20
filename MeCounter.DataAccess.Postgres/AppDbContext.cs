using MeCounter.DataAccess.Postgres.Configurations;
using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;

namespace MeCounter.DataAccess.Postgres;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        //Database.Migrate();
    }
    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Porn> Porns => Set<Porn>();
    public DbSet<VideoMetadata> VideoMetadatas => Set<VideoMetadata>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ChatConfig());
        modelBuilder.ApplyConfiguration(new UserConfig());

        base.OnModelCreating(modelBuilder);
    }
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    if (!optionsBuilder.IsConfigured)
    //    {
    //        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=passw0rd;Database=telegrambot_db");
    //    }
    //}
}
