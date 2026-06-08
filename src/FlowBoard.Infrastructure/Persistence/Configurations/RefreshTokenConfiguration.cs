using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).ValueGeneratedNever();

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.Property(rt => rt.FamilyId).IsRequired();
        builder.HasIndex(rt => rt.FamilyId);

        builder.Property(rt => rt.ExpiresAt).IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt).IsRequired();

        // Matching soft-delete query filter — when a user is soft-deleted, their refresh tokens
        // are also excluded from queries. Without this, EF Core warns that User has a query filter
        // but RefreshToken (with a required User navigation) does not, leading to unexpected results.
        builder.HasQueryFilter(rt => !rt.User.IsDeleted);

        // Ignore navigation back to domain events
        builder.Ignore(rt => rt.DomainEvents);
    }
}
