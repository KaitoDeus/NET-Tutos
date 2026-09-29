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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserLessonProgress> LessonProgresses { get; set; } = new List<UserLessonProgress>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}
