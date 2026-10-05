namespace Evaluation.UserDeactivation;

public sealed class UserAccessService
{
    public bool CanLogin(User user) => user.IsActive;
}
