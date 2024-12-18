using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DatabaseContext
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsStudent { get; set; }
    }

    public class Student
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public StudentStatus Status { get; set; }
        public string Group { get; set; }
    }

    public enum StudentStatus
    {
        active, pendingApproval, fired
    }

    public class AppDbContext : DbContext
    {
        public static AppDbContext Instance;

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(
                    "Server=localhost;Database=kafedrabd;User=root;",
                    new MySqlServerVersion(new Version(8, 0, 29))
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IsStudent).IsRequired();
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StudentName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(15);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(100);
            });
        }
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql("Server=localhost;Database=kafedrabd;User=root;", new MySqlServerVersion(new Version(8, 0, 29)));
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
