using System.ComponentModel.DataAnnotations;

namespace MeCounter.DataAccess.Postgres.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public bool IsAdmin { get; set; } = false;
        public bool IsCounted { get; set; } = true;
        public int WordCount { get; set; } = 0;

        // Навигационное свойство: EF Core автоматически создаст таблицу ChatUser
        public ICollection<Chat> Chats { get; set; } = [];
    }
}