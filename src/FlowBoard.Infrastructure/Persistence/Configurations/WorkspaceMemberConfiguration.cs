using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        // Composite uniqueness: a user can only have one role per workspace
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId); // for "workspaces I belong to" lookups

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.WorkspaceId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();

        // Enum stored as string — adding new roles never reorders existing rows
        builder.Property(m => m.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(m => m.DomainEvents);
    }
}