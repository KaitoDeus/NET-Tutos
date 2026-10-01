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
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<CodingChallenge> CodingChallenges => Set<CodingChallenge>();
    public DbSet<CodeTestCase> CodeTestCases => Set<CodeTestCase>();
    public DbSet<CodeSubmission> CodeSubmissions => Set<CodeSubmission>();
    public DbSet<DiscussionComment> DiscussionComments => Set<DiscussionComment>();
    public DbSet<CommentUpvote> CommentUpvotes => Set<CommentUpvote>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();

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

        // QuizAttempt configurations
        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasOne(a => a.User)
                  .WithMany(u => u.QuizAttempts)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Category)
                  .WithMany()
                  .HasForeignKey(a => a.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Certificate configurations
        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.CertificateCode).IsUnique();

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Certificates)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Category)
                  .WithMany()
                  .HasForeignKey(c => c.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // CodingChallenge configurations
        modelBuilder.Entity<CodingChallenge>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Slug).IsUnique();

            entity.HasOne(c => c.Category)
                  .WithMany()
                  .HasForeignKey(c => c.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // CodeTestCase configurations
        modelBuilder.Entity<CodeTestCase>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasOne(t => t.CodingChallenge)
                  .WithMany(c => c.TestCases)
                  .HasForeignKey(t => t.CodingChallengeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CodeSubmission configurations
        modelBuilder.Entity<CodeSubmission>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasOne(s => s.User)
                  .WithMany(u => u.Submissions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.CodingChallenge)
                  .WithMany(c => c.Submissions)
                  .HasForeignKey(s => s.CodingChallengeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscussionComment configurations
        modelBuilder.Entity<DiscussionComment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ContentMarkdown).IsRequired().HasMaxLength(4000);

            entity.HasOne(c => c.Tutorial)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(c => c.TutorialId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.CodingChallenge)
                  .WithMany(c => c.Comments)
                  .HasForeignKey(c => c.CodingChallengeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Comments)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.ParentComment)
                  .WithMany(p => p.Replies)
                  .HasForeignKey(c => c.ParentCommentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CommentUpvote configurations
        modelBuilder.Entity<CommentUpvote>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => new { u.CommentId, u.UserId }).IsUnique();

            entity.HasOne(u => u.Comment)
                  .WithMany(c => c.Upvotes)
                  .HasForeignKey(u => u.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.User)
                  .WithMany(u => u.Upvotes)
                  .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserBadge configurations
        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => new { b.UserId, b.BadgeCode }).IsUnique();

            entity.HasOne(b => b.User)
                  .WithMany(u => u.Badges)
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
