using FlowBoard.Domain.Entities;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();

        builder.Property(w => w.Slug)
            .HasConversion(
                slug => slug.Value,
                value => WorkspaceSlug.FromTrustedSource(value))
            .HasMaxLength(60)
            .IsRequired();

        builder.HasIndex(w => w.Slug).IsUnique();
        builder.HasIndex(w => w.OwnerId);

        builder.Property(w => w.OwnerId).IsRequired();
        builder.Property(w => w.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(w => w.CreatedAt).IsRequired();
        builder.Property(w => w.UpdatedAt).IsRequired();

        // Owning collection — Members are part of the Workspace aggregate
        builder.HasMany(w => w.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(w => !w.IsDeleted);

        builder.Ignore(w => w.DomainEvents);
    }
}