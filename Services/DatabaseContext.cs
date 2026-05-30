using Microsoft.EntityFrameworkCore;
using NodeTracker.Models;

namespace NodeTracker.Services
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<NodeTracker.Models.User> Users { get; set; } = null!;

        private readonly string _databasePath;

        public DatabaseContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Content);
                entity.Property(e => e.Tags);
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsFavorite).HasDefaultValue(false);
            });

            modelBuilder.Entity<NodeTracker.Models.User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.PasswordHash).IsRequired();
            });
        }
    }
}
