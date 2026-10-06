using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.Data;

public static class MockUserSeeder
{
    private static readonly string[] LastNames = new[]
    {
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đoàn", "Đinh", "Mai", "Trịnh",
        "Lương", "Thái", "Hà", "Cao", "Tô", "Trương", "Phùng", "Tạ", "Quách", "Diệp"
    };

    private static readonly string[] MiddleNamesMale = new[]
    {
        "Văn", "Hữu", "Đức", "Minh", "Hoàng", "Thanh", "Quang", "Quốc", "Đình", "Trọng",
        "Tiến", "Anh", "Bảo", "Tuấn", "Công", "Phúc", "Duy", "Hải", "Xuân", "Khắc",
        "Thành", "Gia", "Đăng", "Nhật", "Hùng"
    };

    private static readonly string[] MiddleNamesFemale = new[]
    {
        "Thị", "Ngọc", "Thanh", "Thùy", "Phương", "Khánh", "Mỹ", "Ánh", "Hồng", "Thu",
        "Mai", "Kim", "Như", "Bích", "Tuyết", "Hải", "Tường", "Diệu", "Yến", "Lan"
    };

    private static readonly string[] FirstNamesMale = new[]
    {
        "Nam", "Tuấn", "Hùng", "Long", "Dũng", "Kiên", "Quân", "Thắng", "Huy", "Khang",
        "Lâm", "Sơn", "Đạt", "Khoa", "Nhân", "Phong", "Cường", "Thịnh", "Phát", "Triết",
        "Hiếu", "Trung", "Tùng", "Nghĩa", "Lộc", "Trí", "Bách", "Việt", "Toàn", "Khôi",
        "Minh", "Duy", "Hoàng", "Thành", "An", "Bình", "Châu", "Vinh", "Phúc", "Tú"
    };

    private static readonly string[] FirstNamesFemale = new[]
    {
        "Lan", "Linh", "Mai", "Hương", "Trang", "Thảo", "Ngọc", "Hà", "My", "Quỳnh",
        "Phương", "Vy", "Trâm", "Châu", "Nhi", "Yến", "Hoa", "Ly", "Dung", "Hằng",
        "Nga", "Oanh", "Thư", "Tiên", "Uyên", "Vân", "Anh", "Ngân", "Ánh", "Trang",
        "Diệp", "Huyền", "Tú", "Bích", "Chi", "Quyên", "Tâm", "Thương", "Trinh", "Tuyết"
    };

    private static readonly string[] EmailDomains = new[]
    {
        "gmail.com", "outlook.com", "hotmail.com", "yahoo.com", "fpt.edu.vn",
        "hust.edu.vn", "uit.edu.vn", "techmail.vn", "vnu.edu.vn", "nettutos.edu.vn"
    };

    private static readonly string[] Bios = new[]
    {
        "Lập trình viên Backend .NET đam mê xây dựng hệ thống phân tán quy mô lớn.",
        "Sinh viên năm cuối ngành CNTT đang chinh phục C# & ASP.NET Core MVC.",
        "Software Engineer tìm hiểu sâu về Clean Architecture, CQRS và Domain-Driven Design.",
        "Đam mê lập trình bất đồng bộ async/await, LINQ và tối ưu hóa hiệu năng ứng dụng .NET.",
        "Yêu thích giải thử thách thuật toán C# và thiết kế Microservices với Docker.",
        "Học viên NET-Tutos kiên trì theo đuổi lộ trình trở thành Senior .NET Backend Developer.",
        "Backend Developer chuyên sâu về thiết kế RESTful Web API và Entity Framework Core.",
        "Thành viên tích cực cộng đồng C# Việt Nam, hướng tới các kỳ thi chứng chỉ Microsoft.",
        "Đang nghiên cứu realtime app với SignalR và lập trình đám mây Azure / AWS.",
        "Yêu thích mã nguồn mở, Clean Code và các Design Patterns hướng đối tượng.",
        "Lập trình viên chuyển ngành, đam mê sự mạch lạc và mạnh mẽ của hệ sinh thái .NET.",
        "Fullstack .NET Developer đang phát triển các dự án thực tế cho doanh nghiệp."
    };

    private static readonly string[] DiscussionQuestions = new[]
    {
        "Mọi người cho mình hỏi khi nào nên dùng `IEnumerable<T>` thay vì `IQueryable<T>` trong Entity Framework Core?",
        "Làm thế nào để xử lý Race Condition khi nhiều client cùng gửi request trừ số dư tài khoản cùng 1 lúc trong ASP.NET Core?",
        "Sự khác biệt thực sự giữa `AddScoped`, `AddTransient` và `AddSingleton` trong thực tế dự án là gì vậy ạ?",
        "Trong Clean Architecture, tại sao tầng Domain không được phép phụ thuộc vào bất kỳ package NuGet bên ngoài nào?",
        "Làm sao để cấu hình SignalR Hub tự động scale out khi chạy trên nhiều máy chủ (Redis Backplane)?",
        "Các anh chị cho em hỏi có nên dùng AutoMapper trong các dự án lớn hay nên tự viết tay Mapping thủ công để tối ưu tốc độ?",
        "Cách tốt nhất để implement Refresh Token an toàn khi kết hợp với JWT trong ASP.NET Core Web API là gì?",
        "Khi thực hiện các tác vụ nặng, khi nào thì nên dùng BackgroundService và khi nào nên dùng Hangfire?",
        "Mọi người thường tổ chức Unit Test cho các tầng Service và Controller như thế nào để đạt độ bao phủ cao?",
        "Ưu và nhược điểm của việc dùng Guid làm Khóa chính (Primary Key) so với ID tự tăng (Identity Int) trong SQL Server?"
    };

    private static readonly string[] DiscussionReplies = new[]
    {
        "`IQueryable<T>` sẽ dịch câu truy vấn sang SQL và thực thi trực tiếp trên database server, giúp tối ưu băng thông. Còn `IEnumerable<T>` sẽ load toàn bộ dữ liệu về bộ nhớ RAM rồi mới lọc client-side.",
        "Bạn nên sử dụng Optimistic Concurrency Control (với `RowVersion` / `[Timestamp]`) hoặc dùng Pessimistic Locking với câu lệnh `SELECT ... FOR UPDATE` trong transaction.",
        "`Transient` tạo mới mỗi lần được gọi, `Scoped` tạo 1 instance duy nhất trong cùng 1 HTTP Request, còn `Singleton` sống suốt vòng đời của cả ứng dụng.",
        "Domain là trái tim của hệ thống. Nếu Domain phụ thuộc thư viện ngoài, khi thư viện đó thay đổi hoặc bị deprecate thì toàn bộ core logic của bạn sẽ bị ảnh hưởng, vi phạm nguyên lý Dependency Inversion.",
        "Bạn có thể dùng package `Microsoft.AspNetCore.SignalR.StackExchangeRedis` để các server chia sẻ chung message stream qua Redis Pub/Sub rất mượt mà.",
        "Với các dự án yêu cầu hiệu năng cực cao, bạn có thể cân nhắc dùng Mapperly (Source Generator sinh code tại compile time) thay cho AutoMapper để không tốn chi phí Reflection.",
        "Nên lưu Refresh Token trong Database có kèm cờ `IsRevoked`, `ExpiresAt` và gửi nó về client dưới dạng `HttpOnly` Cookie an toàn để chống XSS.",
        "Với tác vụ đơn giản chạy định kỳ thì `BackgroundService` là đủ. Còn tác vụ cần retry khi lỗi, có dashboard theo dõi trạng thái thì Hangfire là lựa chọn số 1.",
        "Nên dùng `xUnit` kết hợp với `Moq` để mock các Repository và DbContext. Tập trung test kỹ các kịch bản biên (edge cases) và validate nghiệp vụ.",
        "Guid giúp sinh ID độc lập ở client mà không sợ trùng, rất tốt cho hệ thống phân tán, nhưng sẽ gây phân mảnh index (B-Tree Fragmentation). Bạn nên dùng Sequential Guid (CombGuid) để khắc phục."
    };

    public static async Task SeedMockUsersAsync(AppDbContext context, RoleManager<IdentityRole> roleManager, int targetCount = 1000)
    {
        int currentCount = await context.Users.CountAsync();
        if (currentCount >= targetCount)
        {
            return;
        }

        int toGenerate = targetCount - currentCount;

        // Ensure "Student" role exists
        var studentRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        if (studentRole == null)
        {
            studentRole = new IdentityRole("Student");
            await roleManager.CreateAsync(studentRole);
        }

        // Precompute default password hash once for all 1000 mock users: "StudentPassword@123"
        var hasher = new PasswordHasher<ApplicationUser>();
        var defaultPasswordHash = hasher.HashPassword(null!, "StudentPassword@123");

        // Load existing entities for relational links
        var tutorials = await context.Tutorials.OrderBy(t => t.OrderIndex).ToListAsync();
        var challenges = await context.CodingChallenges.ToListAsync();
        var capstones = await context.CapstoneProjects.ToListAsync();
        var categories = await context.Categories.ToListAsync();

        var existingEmails = (await context.Users.Select(u => u.Email).ToListAsync())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var random = new Random(1024);
        var today = DateTime.UtcNow.Date;

        var users = new List<ApplicationUser>(toGenerate);
        var userRoles = new List<IdentityUserRole<string>>(toGenerate);
        var lessonProgresses = new List<UserLessonProgress>();
        var checkIns = new List<DailyCheckIn>();
        var badges = new List<UserBadge>();
        var submissions = new List<CodeSubmission>();
        var certificates = new List<Certificate>();
        var projectSubmissions = new List<ProjectSubmission>();

        for (int i = 0; i < toGenerate; i++)
        {
            bool isMale = random.Next(2) == 0;
            string lastName = LastNames[random.Next(LastNames.Length)];
            string middleName = isMale
                ? MiddleNamesMale[random.Next(MiddleNamesMale.Length)]
                : MiddleNamesFemale[random.Next(MiddleNamesFemale.Length)];
            string firstName = isMale
                ? FirstNamesMale[random.Next(FirstNamesMale.Length)]
                : FirstNamesFemale[random.Next(FirstNamesFemale.Length)];

            string fullName = $"{lastName} {middleName} {firstName}";

            // Generate clean unique email
            string asciiFirst = RemoveDiacritics(firstName).ToLowerInvariant();
            string asciiLast = RemoveDiacritics(lastName).ToLowerInvariant();
            string domain = EmailDomains[random.Next(EmailDomains.Length)];

            string email;
            int counter = i + 1;
            do
            {
                email = $"{asciiFirst}.{asciiLast}{counter}@{domain}";
                counter++;
            } while (existingEmails.Contains(email));
            existingEmails.Add(email);

            string userId = Guid.NewGuid().ToString("D");
            DateTime createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 150)).AddHours(-random.Next(1, 24));

            // Determine user activity Tier (Power Law distribution)
            int xp;
            int completedLessonCount;
            int solvedChallengeCount;
            int currentStreak;
            int longestStreak;

            if (i < 15) // Tier 1 (Top 1.5% - Elite Leaderboard Champions)
            {
                xp = random.Next(1800, 3600);
                completedLessonCount = Math.Min(tutorials.Count, random.Next(12, 17));
                solvedChallengeCount = Math.Min(challenges.Count, random.Next(4, 7));
                currentStreak = random.Next(12, 35);
                longestStreak = Math.Max(currentStreak, random.Next(25, 45));
            }
            else if (i < 65) // Tier 2 (Top 6.5% - High Achievers)
            {
                xp = random.Next(800, 1800);
                completedLessonCount = Math.Min(tutorials.Count, random.Next(8, 14));
                solvedChallengeCount = Math.Min(challenges.Count, random.Next(3, 5));
                currentStreak = random.Next(5, 14);
                longestStreak = Math.Max(currentStreak, random.Next(10, 20));
            }
            else if (i < 200) // Tier 3 (Top 20% - Regular Active Learners)
            {
                xp = random.Next(350, 800);
                completedLessonCount = Math.Min(tutorials.Count, random.Next(4, 9));
                solvedChallengeCount = Math.Min(challenges.Count, random.Next(1, 3));
                currentStreak = random.Next(2, 7);
                longestStreak = Math.Max(currentStreak, random.Next(4, 12));
            }
            else if (i < 550) // Tier 4 (Next 35% - Intermediate Learners)
            {
                xp = random.Next(100, 350);
                completedLessonCount = Math.Min(tutorials.Count, random.Next(2, 5));
                solvedChallengeCount = random.Next(2) == 0 ? 1 : 0;
                currentStreak = random.Next(0, 4);
                longestStreak = Math.Max(currentStreak, random.Next(2, 6));
            }
            else // Tier 5 (Remaining 45% - Newbies & Casual Explorers)
            {
                xp = random.Next(20, 100);
                completedLessonCount = Math.Min(tutorials.Count, random.Next(1, 3));
                solvedChallengeCount = 0;
                currentStreak = random.Next(0, 2);
                longestStreak = Math.Max(currentStreak, 1);
            }

            DateTime? lastCheckInDate = currentStreak > 0
                ? (random.Next(3) == 0 ? today.AddDays(-1) : today)
                : null;

            string bio = Bios[random.Next(Bios.Length)];

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                PasswordHash = defaultPasswordHash,
                SecurityStamp = Guid.NewGuid().ToString("D"),
                ConcurrencyStamp = Guid.NewGuid().ToString("D"),
                FullName = fullName,
                Bio = bio,
                ExperiencePoints = xp,
                CurrentStreak = currentStreak,
                LongestStreak = longestStreak,
                LastCheckInDate = lastCheckInDate,
                CreatedAt = createdAt
            };

            users.Add(user);

            // Add role association
            userRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = studentRole.Id
            });

            // 1. Seed Lesson Progresses
            if (tutorials.Count > 0 && completedLessonCount > 0)
            {
                var assignedTutorials = tutorials.Take(completedLessonCount).ToList();
                for (int tIdx = 0; tIdx < assignedTutorials.Count; tIdx++)
                {
                    var tut = assignedTutorials[tIdx];
                    lessonProgresses.Add(new UserLessonProgress
                    {
                        UserId = userId,
                        TutorialId = tut.Id,
                        IsCompleted = true,
                        CompletedAt = createdAt.AddDays(tIdx * 2).AddHours(random.Next(1, 10)),
                        LastAccessedAt = createdAt.AddDays(tIdx * 2).AddHours(random.Next(1, 10))
                    });
                }
            }

            // 2. Seed Daily Check-ins
            if (currentStreak > 0 && lastCheckInDate.HasValue)
            {
                for (int s = 0; s < Math.Min(currentStreak, 10); s++)
                {
                    checkIns.Add(new DailyCheckIn
                    {
                        UserId = userId,
                        CheckInDate = lastCheckInDate.Value.AddDays(-s),
                        StreakDay = currentStreak - s,
                        XpEarned = 10 + (s % 5) * 5,
                        CreatedAt = lastCheckInDate.Value.AddDays(-s).AddHours(random.Next(7, 21))
                    });
                }
            }

            // 3. Seed Badges
            if (completedLessonCount >= 1)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "NEWBIE",
                    Title = "Tân Binh .NET",
                    Description = "Bắt đầu hành trình và hoàn thành bài học lý thuyết đầu tiên",
                    IconClass = "bi-rocket-takeoff-fill",
                    ColorClass = "primary",
                    EarnedAt = createdAt.AddDays(1)
                });
            }

            if (completedLessonCount >= 5)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "SCHOLAR",
                    Title = "Học Giả Chăm Chỉ",
                    Description = "Kiên trì tích lũy kiến thức và hoàn thành từ 5 bài học",
                    IconClass = "bi-book-half",
                    ColorClass = "info",
                    EarnedAt = createdAt.AddDays(10)
                });
            }

            if (solvedChallengeCount >= 1)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "CODER",
                    Title = "Thợ Săn Thuật Toán",
                    Description = "Thực thi và vượt qua thử thách lập trình C# đầu tiên",
                    IconClass = "bi-code-slash",
                    ColorClass = "warning",
                    EarnedAt = createdAt.AddDays(12)
                });
            }

            if (solvedChallengeCount >= 3)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "ALGO_MASTER",
                    Title = "Cao Thủ Thuật Toán",
                    Description = "Chinh phục từ 3 thử thách thuật toán trong C# Sandbox",
                    IconClass = "bi-lightning-charge-fill",
                    ColorClass = "danger",
                    EarnedAt = createdAt.AddDays(25)
                });
            }

            if (longestStreak >= 3)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "STREAK_3",
                    Title = "Ngọn Lửa Bền Bỉ",
                    Description = "Duy trì chuỗi học tập 3 ngày liên tục",
                    IconClass = "bi-fire",
                    ColorClass = "danger",
                    EarnedAt = createdAt.AddDays(5)
                });
            }

            if (longestStreak >= 7)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "STREAK_7",
                    Title = "Chiến Binh Kỷ Luật",
                    Description = "Duy trì chuỗi học tập 7 ngày liên tiếp không nghỉ",
                    IconClass = "bi-shield-check",
                    ColorClass = "warning",
                    EarnedAt = createdAt.AddDays(14)
                });
            }

            if (longestStreak >= 30)
            {
                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "STREAK_30",
                    Title = "Huyền Thoại Bất Bại",
                    Description = "Kỷ lục 30 ngày kiên trì học tập liên tục cùng .NET",
                    IconClass = "bi-trophy-fill",
                    ColorClass = "primary",
                    EarnedAt = createdAt.AddDays(40)
                });
            }

            // 4. Seed Coding Challenge Submissions
            if (challenges.Count > 0 && solvedChallengeCount > 0)
            {
                for (int cIdx = 0; cIdx < solvedChallengeCount; cIdx++)
                {
                    var ch = challenges[cIdx];
                    submissions.Add(new CodeSubmission
                    {
                        UserId = userId,
                        CodingChallengeId = ch.Id,
                        SubmittedCode = "// Solution submitted by " + fullName + Environment.NewLine + ch.InitialCode,
                        IsPassed = true,
                        PassedTestsCount = 3,
                        TotalTestsCount = 3,
                        ExecutionTimeMs = random.Next(15, 65),
                        XpEarned = ch.XpReward,
                        SubmittedAt = createdAt.AddDays(cIdx * 5).AddHours(random.Next(1, 20))
                    });
                }
            }

            // 5. Seed Certificates for Top Users
            if (i < 25 && categories.Count > 0)
            {
                var cat = categories[random.Next(categories.Count)];
                string certCode = $"CERT-NET-{2026}-{random.Next(10000, 99999)}";
                certificates.Add(new Certificate
                {
                    CertificateCode = certCode,
                    UserId = userId,
                    CategoryId = cat.Id,
                    CourseTitle = $"Khóa học Chuyên sâu {cat.Name}",
                    StudentFullName = fullName,
                    FinalScore = random.Next(88, 100),
                    IssuedAt = createdAt.AddDays(random.Next(20, 60)),
                    VerificationUrl = $"/verify-certificate/{certCode}"
                });

                badges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeCode = "CERTIFIED",
                    Title = "Bậc Thầy Chứng Chỉ",
                    Description = "Vượt qua kỳ thi tốt nghiệp và nhận chứng chỉ số chính thức",
                    IconClass = "bi-award-fill",
                    ColorClass = "success",
                    EarnedAt = createdAt.AddDays(25)
                });
            }

            // 6. Seed Capstone Project Submissions for Select Users
            if (capstones.Count > 0 && i < 35)
            {
                var prj = capstones[random.Next(capstones.Count)];
                bool isApproved = i < 20;

                projectSubmissions.Add(new ProjectSubmission
                {
                    ProjectId = prj.Id,
                    UserId = userId,
                    GitHubRepoUrl = $"https://github.com/{asciiFirst}-{asciiLast}/{prj.Slug}",
                    LiveDemoUrl = isApproved ? $"https://{asciiFirst}-{asciiLast}-demo.azurewebsites.net" : null,
                    Notes = $"Dự án hoàn thành bởi {fullName}. Áp dụng Clean Architecture, cấu trúc các tầng Domain, Application, Infrastructure và Web API độc lập.",
                    Status = isApproved ? ProjectSubmissionStatus.Approved : ProjectSubmissionStatus.Submitted,
                    Score = isApproved ? random.Next(85, 98) : null,
                    ReviewerFeedback = isApproved
                        ? "Mã nguồn tổ chức bài bản, clean code, tuân thủ nguyên lý SOLID. Cấu trúc thư mục rõ ràng, xử lý lỗi ExceptionFilter chuẩn mực. Xuất sắc!"
                        : null,
                    SubmittedAt = createdAt.AddDays(random.Next(15, 45)),
                    ReviewedAt = isApproved ? createdAt.AddDays(random.Next(16, 48)) : null,
                    XpAwarded = isApproved ? prj.RewardXp + 50 : 0
                });

                if (isApproved)
                {
                    badges.Add(new UserBadge
                    {
                        UserId = userId,
                        BadgeCode = "ARCHITECT",
                        Title = "Kiến Trúc Sư .NET",
                        Description = "Bảo vệ thành công Đồ án Thực chiến Capstone và được duyệt đạt chuẩn",
                        IconClass = "bi-diagram-3-fill",
                        ColorClass = "success",
                        EarnedAt = createdAt.AddDays(48)
                    });
                }
            }
        }

        // Add users and associated records in chunks for optimum SQLite throughput
        await context.Users.AddRangeAsync(users);
        await context.UserRoles.AddRangeAsync(userRoles);
        await context.SaveChangesAsync();

        // Add child collections in batches
        const int batchSize = 1000;

        for (int b = 0; b < lessonProgresses.Count; b += batchSize)
        {
            await context.UserLessonProgresses.AddRangeAsync(lessonProgresses.Skip(b).Take(batchSize));
            await context.SaveChangesAsync();
        }

        for (int b = 0; b < checkIns.Count; b += batchSize)
        {
            await context.DailyCheckIns.AddRangeAsync(checkIns.Skip(b).Take(batchSize));
            await context.SaveChangesAsync();
        }

        for (int b = 0; b < badges.Count; b += batchSize)
        {
            await context.UserBadges.AddRangeAsync(badges.Skip(b).Take(batchSize));
            await context.SaveChangesAsync();
        }

        if (submissions.Count > 0)
        {
            await context.CodeSubmissions.AddRangeAsync(submissions);
            await context.SaveChangesAsync();
        }

        if (certificates.Count > 0)
        {
            await context.Certificates.AddRangeAsync(certificates);
            await context.SaveChangesAsync();
        }

        if (projectSubmissions.Count > 0)
        {
            await context.ProjectSubmissions.AddRangeAsync(projectSubmissions);
            await context.SaveChangesAsync();
        }

        // 7. Seed ~50 Realistic Discussion Comments from various users
        if (tutorials.Count > 0 && users.Count > 50)
        {
            var comments = new List<DiscussionComment>();
            for (int q = 0; q < Math.Min(DiscussionQuestions.Length, 10); q++)
            {
                var askUser = users[random.Next(0, 50)];
                var replyUser = users[random.Next(50, 100)];
                var tut = tutorials[random.Next(tutorials.Count)];

                var qComment = new DiscussionComment
                {
                    TutorialId = tut.Id,
                    UserId = askUser.Id,
                    ContentMarkdown = DiscussionQuestions[q],
                    UpvotesCount = random.Next(2, 12),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(2, 10)).AddHours(-random.Next(1, 20))
                };
                comments.Add(qComment);

                var rComment = new DiscussionComment
                {
                    TutorialId = tut.Id,
                    UserId = replyUser.Id,
                    ParentComment = qComment,
                    ContentMarkdown = DiscussionReplies[q],
                    IsBestAnswer = q % 2 == 0,
                    UpvotesCount = random.Next(5, 20),
                    CreatedAt = qComment.CreatedAt.AddHours(random.Next(1, 8))
                };
                comments.Add(rComment);
            }

            await context.DiscussionComments.AddRangeAsync(comments);
            await context.SaveChangesAsync();
        }

        // 8. Seed ~40 vibrant Activity Feed entries across the last 24-48 hours
        if (users.Count > 30)
        {
            var recentActivities = new List<ActivityFeedItem>();
            for (int a = 0; a < 40; a++)
            {
                var u = users[random.Next(Math.Min(users.Count, 80))];
                var time = DateTime.UtcNow.AddMinutes(-random.Next(5, 2880));

                int actType = random.Next(5);
                switch (actType)
                {
                    case 0:
                        var tut = tutorials[random.Next(tutorials.Count)];
                        recentActivities.Add(new ActivityFeedItem
                        {
                            UserId = u.Id,
                            UserDisplayName = u.FullName,
                            Type = ActivityType.LessonCompleted,
                            Title = "đã hoàn thành bài học",
                            Description = tut.Title,
                            TargetUrl = $"/bai-hoc/{tut.Slug}",
                            XpEarned = 20,
                            CreatedAt = time
                        });
                        break;
                    case 1:
                        if (challenges.Count > 0)
                        {
                            var ch = challenges[random.Next(challenges.Count)];
                            recentActivities.Add(new ActivityFeedItem
                            {
                                UserId = u.Id,
                                UserDisplayName = u.FullName,
                                Type = ActivityType.ChallengeSolved,
                                Title = "đã giải thành công thử thách thuật toán",
                                Description = $"{ch.Title} ({ch.XpReward} XP)",
                                TargetUrl = "/Playground/Challenges",
                                XpEarned = ch.XpReward,
                                CreatedAt = time
                            });
                        }
                        break;
                    case 2:
                        recentActivities.Add(new ActivityFeedItem
                        {
                            UserId = u.Id,
                            UserDisplayName = u.FullName,
                            Type = ActivityType.StreakAchieved,
                            Title = "đã đạt chuỗi ngọn lửa học tập",
                            Description = $"{random.Next(3, 15)} ngày kiên trì liên tục 🔥",
                            TargetUrl = "/Streak",
                            XpEarned = 25,
                            CreatedAt = time
                        });
                        break;
                    case 3:
                        recentActivities.Add(new ActivityFeedItem
                        {
                            UserId = u.Id,
                            UserDisplayName = u.FullName,
                            Type = ActivityType.BadgeEarned,
                            Title = "đã mở khóa thành tích mới",
                            Description = "Thợ Săn Thuật Toán C# 🏆",
                            TargetUrl = "/Leaderboard",
                            BadgeCode = "CODER",
                            XpEarned = 50,
                            CreatedAt = time
                        });
                        break;
                    case 4:
                        if (capstones.Count > 0)
                        {
                            var prj = capstones[random.Next(capstones.Count)];
                            recentActivities.Add(new ActivityFeedItem
                            {
                                UserId = u.Id,
                                UserDisplayName = u.FullName,
                                Type = ActivityType.ProjectApproved,
                                Title = "đã hoàn thành xuất sắc đồ án thực chiến",
                                Description = $"{prj.Title} (95/100 điểm) 🎯",
                                TargetUrl = $"/Project/Details/{prj.Slug}",
                                XpEarned = prj.RewardXp + 50,
                                CreatedAt = time
                            });
                        }
                        break;
                }
            }

            await context.ActivityFeedItems.AddRangeAsync(recentActivities);
            await context.SaveChangesAsync();
        }
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
            .Replace("đ", "d").Replace("Đ", "D")
            .Replace(" ", "");
    }
}
