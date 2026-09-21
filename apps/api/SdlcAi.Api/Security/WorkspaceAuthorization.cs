namespace SdlcAi.Api.Security;

public enum WorkspaceRole { Viewer, ProductOwner, Qa, Developer, Admin }

public sealed record ActorContext(string ActorId, Guid WorkspaceId, WorkspaceRole Role);

public interface IActorContextAccessor { ActorContext Current { get; } }

public sealed class DevelopmentActorContextAccessor : IActorContextAccessor
{
    public ActorContext Current => new("local-user", Guid.Empty, WorkspaceRole.Admin);
}

public static class WorkspaceAuthorization
{
    public static bool CanApproveRequirements(WorkspaceRole role) => role is WorkspaceRole.ProductOwner or WorkspaceRole.Admin;
    public static bool CanApproveQa(WorkspaceRole role) => role is WorkspaceRole.Qa or WorkspaceRole.Admin;
    public static bool CanApproveDevelopment(WorkspaceRole role) => role is WorkspaceRole.Developer or WorkspaceRole.Admin;
}
