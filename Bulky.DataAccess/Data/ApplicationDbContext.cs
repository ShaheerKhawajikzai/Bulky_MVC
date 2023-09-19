using Bulky.Models.Models;
using Microsoft.EntityFrameworkCore;


namespace Bulky.DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(

                new Category { Id = 1, DisplayOrder = 1, Name = "Action" },
                new Category { Id = 2, DisplayOrder = 2, Name = "Scifi" },
                new Category { Id = 3, DisplayOrder = 3, Name = "History" }

                );
        }
    }
}
