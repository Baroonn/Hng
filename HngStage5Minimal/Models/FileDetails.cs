using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HngStage5Minimal.Models
{
    public class FileDetails
    {
        public int Id { get; set; }
        [Required]
        public string? Filename { get; set; }
        public bool IsReady { get; set; } = false;
    }

    class HngDb : DbContext
    {
        public HngDb(DbContextOptions options) : base(options) { }
        public DbSet<FileDetails> Files { get; set; } = null!;
    }
}
