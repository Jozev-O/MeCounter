using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeCounter.DataAccess.Postgres.Configurations
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Внешний ключ поле UserId
            builder.HasKey(u => u.UserId);

            builder
                .HasMany(c => c.Chats)
                .WithMany(u => u.Users);
        }
    }
}
