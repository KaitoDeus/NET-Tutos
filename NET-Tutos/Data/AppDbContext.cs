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
    public DbSet<DailyCheckIn> DailyCheckIns => Set<DailyCheckIn>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<ActivityFeedItem> ActivityFeedItems => Set<ActivityFeedItem>();
    public DbSet<CapstoneProject> CapstoneProjects => Set<CapstoneProject>();
    public DbSet<ProjectSubmission> ProjectSubmissions => Set<ProjectSubmission>();
    public DbSet<InterviewFlashcard> InterviewFlashcards => Set<InterviewFlashcard>();
    public DbSet<UserFlashcardProgress> UserFlashcardProgresses => Set<UserFlashcardProgress>();

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

        // DailyCheckIn configurations
        modelBuilder.Entity<DailyCheckIn>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => new { d.UserId, d.CheckInDate }).IsUnique();

            entity.HasOne(d => d.User)
                  .WithMany(u => u.DailyCheckIns)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // UserNotification configurations
        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.HasIndex(n => new { n.UserId, n.IsRead });
            entity.Property(n => n.Title).IsRequired().HasMaxLength(250);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityFeedItem configurations
        modelBuilder.Entity<ActivityFeedItem>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CreatedAt);
            entity.Property(a => a.Title).IsRequired().HasMaxLength(250);
            entity.Property(a => a.Description).IsRequired().HasMaxLength(500);

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // CapstoneProject configurations
        modelBuilder.Entity<CapstoneProject>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Title).IsRequired().HasMaxLength(250);
            entity.Property(p => p.Slug).IsRequired().HasMaxLength(250);
            entity.Property(p => p.ShortDescription).IsRequired().HasMaxLength(500);
            entity.Property(p => p.TechStack).IsRequired().HasMaxLength(250);

            entity.HasOne(p => p.Category)
                  .WithMany()
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectSubmission configurations
        modelBuilder.Entity<ProjectSubmission>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.ProjectId, s.UserId });
            entity.Property(s => s.GitHubRepoUrl).IsRequired().HasMaxLength(500);
            entity.Property(s => s.LiveDemoUrl).HasMaxLength(500);
            entity.Property(s => s.Notes).IsRequired().HasMaxLength(2000);

            entity.HasOne(s => s.Project)
                  .WithMany(p => p.Submissions)
                  .HasForeignKey(s => s.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.User)
                  .WithMany(u => u.ProjectSubmissions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Reviewer)
                  .WithMany()
                  .HasForeignKey(s => s.ReviewedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // InterviewFlashcard configurations
        modelBuilder.Entity<InterviewFlashcard>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.Slug).IsUnique();
            entity.HasIndex(f => f.Topic);
            entity.HasIndex(f => f.Difficulty);
            entity.Property(f => f.Slug).IsRequired().HasMaxLength(200);
            entity.Property(f => f.QuestionVi).IsRequired().HasMaxLength(500);
            entity.Property(f => f.QuestionEn).IsRequired().HasMaxLength(500);
        });

        // UserFlashcardProgress configurations
        modelBuilder.Entity<UserFlashcardProgress>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.UserId, p.FlashcardId }).IsUnique();
            entity.HasIndex(p => p.NextReviewDate);

            entity.HasOne(p => p.Flashcard)
                  .WithMany(f => f.UserProgresses)
                  .HasForeignKey(p => p.FlashcardId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
