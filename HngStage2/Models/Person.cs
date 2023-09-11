using Microsoft.EntityFrameworkCore;

namespace HngStage2.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    class HngDb : DbContext
    {
        public HngDb(DbContextOptions options) : base(options) { }
        public DbSet<Person> People { get; set; } = null!;
    }
}
