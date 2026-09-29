using NET_Tutos.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace NET_Tutos.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tutorial> Tutorials => Set<Tutorial>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<CodeSnippet> CodeSnippets => Set<CodeSnippet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configurations
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(150);
        });

        // Tutorial configurations
        modelBuilder.Entity<Tutorial>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Slug).IsUnique();
            entity.Property(t => t.Title).IsRequired().HasMaxLength(250);
            entity.Property(t => t.Slug).IsRequired().HasMaxLength(250);

            entity.HasOne(t => t.Category)
                  .WithMany(c => c.Tutorials)
                  .HasForeignKey(t => t.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizQuestion configurations
        modelBuilder.Entity<QuizQuestion>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.HasOne(q => q.Tutorial)
                  .WithMany(t => t.QuizQuestions)
                  .HasForeignKey(q => q.TutorialId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CodeSnippet configurations
        modelBuilder.Entity<CodeSnippet>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasOne(s => s.Tutorial)
                  .WithMany(t => t.CodeSnippets)
                  .HasForeignKey(s => s.TutorialId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

