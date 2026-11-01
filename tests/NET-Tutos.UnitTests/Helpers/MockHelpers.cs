using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using NET_Tutos.Data;
using NET_Tutos.Models.Entities;

namespace NET_Tutos.UnitTests.Helpers;

public static class MockHelpers
{
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"NET_Tutos_TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        return userManager;
    }
}
