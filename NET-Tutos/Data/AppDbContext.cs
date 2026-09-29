using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tutorial> Tutorials => Set<Tutorial>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<CodeSnippet> CodeSnippets => Set<CodeSnippet>();
    public DbSet<UserLessonProgress> UserLessonProgresses => Set<UserLessonProgress>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();

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

        // UserLessonProgress configurations
        modelBuilder.Entity<UserLessonProgress>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.UserId, p.TutorialId }).IsUnique();

            entity.HasOne(p => p.User)
                  .WithMany(u => u.LessonProgresses)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Tutorial)
                  .WithMany()
                  .HasForeignKey(p => p.TutorialId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CourseEnrollment configurations
        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CategoryId }).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Enrollments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
