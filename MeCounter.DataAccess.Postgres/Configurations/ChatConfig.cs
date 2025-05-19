using MeCounter.DataAccess.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeCounter.DataAccess.Postgres.Configurations
{
    public class ChatConfig : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            // Внешний ключ поле ChatId
            builder.HasKey(c => c.ChatId);

            builder
                .HasMany(c => c.Users)
                .WithMany(u => u.Chats);
        }
    }
}
