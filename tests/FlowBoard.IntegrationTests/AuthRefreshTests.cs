using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Security;
using FlowBoard.Application.Features.Auth;
using FlowBoard.Application.Features.Auth.Commands.RefreshToken;
using FlowBoard.Application.Features.Auth.Commands.Register;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// Auth integration tests against real SQL Server — refresh rotation and concurrency.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class AuthRefreshTests(SqlServerFixture fixture)
{
    [SkippableFact]
    public async Task RefreshToken_ConcurrentRequestsWithSameToken_OnlyOneRotationSucceeds()
    {
        Skip.IfNot(fixture.IsDockerAvailable, "Docker is not running — start Docker Desktop to run integration tests.");

        var email = $"refresh-{Guid.NewGuid():N}@test.com";
        var register = await fixture.SendAsync(new RegisterCommand(
            email,
            "Refresh Tester",
            "Password123!",
            "Password123!"));

        var refreshToken = register.RefreshToken;
        var familyId = await GetFamilyIdAsync(refreshToken);

        var tasks = Enumerable.Range(0, 2)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var response = await fixture.SendAsync(new RefreshTokenCommand(refreshToken));
                    return (Success: true, Error: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Success: false, Error: ex);
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r.Success));
        Assert.Equal(1, results.Count(r => !r.Success));
        Assert.All(results.Where(r => !r.Success), r => Assert.IsType<UnauthorizedException>(r.Error));

        var activeCount = await CountActiveTokensInFamilyAsync(familyId);
        Assert.Equal(1, activeCount);
    }

    private Task<Guid> GetFamilyIdAsync(string refreshToken) =>
        ExecuteDbQueryAsync(async db =>
        {
            var hash = TokenHasher.Hash(refreshToken);
            var token = await db.RefreshTokens.SingleAsync(rt => rt.TokenHash == hash);
            return token.FamilyId;
        });

    private Task<int> CountActiveTokensInFamilyAsync(Guid familyId) =>
        ExecuteDbQueryAsync(async db =>
            await db.RefreshTokens.CountAsync(rt => rt.FamilyId == familyId && !rt.IsRevoked));

    private async Task<T> ExecuteDbQueryAsync<T>(Func<FlowBoard.Infrastructure.Persistence.FlowBoardDbContext, Task<T>> query)
    {
        T result = default!;
        await fixture.ExecuteDbAsync(async db => result = await query(db));
        return result;
    }
}
