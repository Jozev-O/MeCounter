using System.ComponentModel.DataAnnotations;

namespace MeCounter.DataAccess.Postgres.Models
{
    public class DiceState
    {
        [Key]
        public long ChatId { get; set; }
        public int LastValue { get; set; }
        public string LastUser { get; set; }
        public string LastMoniker { get; set; }
    }
}