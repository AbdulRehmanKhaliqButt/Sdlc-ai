using Evaluation.UserDeactivation;
using Xunit;

namespace Evaluation.UserDeactivation.Tests;

public sealed class UserTests
{
    [Fact]
    public void Active_user_can_login()
    {
        var user = new User();
        var access = new UserAccessService();

        Assert.True(access.CanLogin(user));
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var user = new User();

        Assert.True(user.Deactivate());
        Assert.False(user.Deactivate());
        Assert.False(user.IsActive);
    }
}
