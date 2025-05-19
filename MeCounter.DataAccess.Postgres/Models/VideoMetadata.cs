using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeCounter.DataAccess.Postgres.Models
{
    public class VideoMetadata
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string? FilePath { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    }
}