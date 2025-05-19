using System.ComponentModel.DataAnnotations;

namespace MeCounter.DataAccess.Postgres.Models
{
    public class Porn
    {
        [Key]
        public long Id { get; set; }
        public Uri? Url { get; set; }
    }
}
