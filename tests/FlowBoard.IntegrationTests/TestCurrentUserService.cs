using FlowBoard.Application.Common.Interfaces;

namespace FlowBoard.IntegrationTests;

/// <summary>Test double for the authenticated user. Set <see cref="UserId"/> to impersonate.</summary>
public sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated => UserId is not null;
}
