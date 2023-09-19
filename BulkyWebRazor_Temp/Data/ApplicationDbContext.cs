using BulkyWebRazor_Temp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BulkyWebRazor_Temp.Data
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
