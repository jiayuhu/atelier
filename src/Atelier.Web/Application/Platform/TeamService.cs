using Atelier.Web.Domain.Platform;

namespace Atelier.Web.Application.Platform;

public sealed class TeamService
{
    public Team Create(Guid workspaceId, string name, Guid leadUserId)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace id is required.", nameof(workspaceId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Team name is required.", nameof(name));
        }

        if (leadUserId == Guid.Empty)
        {
            throw new ArgumentException("Lead user id is required.", nameof(leadUserId));
        }

        return new Team
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            TeamLeadUserId = leadUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool CanAccessTeam(UserRole actorRole, Guid actorTeamId, Guid requestedTeamId)
    {
        return actorRole switch
        {
            UserRole.Administrator => true,
            UserRole.TeamLead => actorTeamId != Guid.Empty && actorTeamId == requestedTeamId,
            _ => false,
        };
    }
}
