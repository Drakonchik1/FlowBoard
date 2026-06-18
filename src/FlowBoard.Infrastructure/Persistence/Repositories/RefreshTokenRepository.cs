using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowBoard.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(FlowBoardDbContext context)
    : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetActiveByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        await DbSet
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == tokenHash && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task<RefreshToken?> GetByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

    public async Task<RefreshToken?> GetByHashWithUpdateLockAsync(
        string tokenHash, CancellationToken cancellationToken = default) =>
        await DbSet
            .FromSqlInterpolated($"""
                SELECT Id, UserId, TokenHash, FamilyId, ExpiresAt, IsRevoked, CreatedAt
                FROM refresh_tokens WITH (UPDLOCK, ROWLOCK)
                WHERE TokenHash = {tokenHash}
                """)
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task RevokeEntireFamilyAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        var familyTokens = await DbSet
            .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in familyTokens)
            token.Revoke();
    }
}
