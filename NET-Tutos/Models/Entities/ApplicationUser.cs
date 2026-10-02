using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NET_Tutos.Models.Entities;

public class ApplicationUser : IdentityUser
{
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    public int ExperiencePoints { get; set; } = 0;

    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;
    public DateTime? LastCheckInDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserLessonProgress> LessonProgresses { get; set; } = new List<UserLessonProgress>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<CodeSubmission> Submissions { get; set; } = new List<CodeSubmission>();
    public ICollection<DiscussionComment> Comments { get; set; } = new List<DiscussionComment>();
    public ICollection<CommentUpvote> Upvotes { get; set; } = new List<CommentUpvote>();
    public ICollection<UserBadge> Badges { get; set; } = new List<UserBadge>();
    public ICollection<DailyCheckIn> DailyCheckIns { get; set; } = new List<DailyCheckIn>();
    public ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
}
