using System.ComponentModel.DataAnnotations;

namespace MeCounter.DataAccess.Postgres.Models
{
    public class Chat
    {
        [Key]
        public long ChatId { get; set; }
        public string? Title { get; set; }

        // Навигационное свойство: EF Core автоматически создаст таблицу ChatUser
        public ICollection<User> Users { get; set; } = [];
    }
}