using BtPlatform.Core.Entities;
using Xunit;

namespace BtPlatform.Tests;

public class AppRolesTests
{
    [Fact]
    public void RoleConstants_MatchEnumNames()
    {
        Assert.Equal(nameof(UserRole.Admin), AppRoles.Admin);
        Assert.Equal(nameof(UserRole.Technician), AppRoles.Technician);
        Assert.Equal(nameof(UserRole.Viewer), AppRoles.Viewer);
    }
}