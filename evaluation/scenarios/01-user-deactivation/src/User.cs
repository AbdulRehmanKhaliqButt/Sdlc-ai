namespace Evaluation.UserDeactivation;

public sealed class User
{
    public Guid Id { get; init; }
    public bool IsActive { get; private set; } = true;

    public bool Deactivate()
    {
        if (!IsActive)
            return false;

        IsActive = false;
        return true;
    }
}
