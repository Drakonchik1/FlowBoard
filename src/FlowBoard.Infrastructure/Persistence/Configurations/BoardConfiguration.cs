using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("boards");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ProjectId).IsRequired();
        builder.Property(b => b.WorkspaceId).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        builder.HasIndex(b => b.ProjectId);
        builder.HasIndex(b => b.WorkspaceId);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.Ignore(b => b.DomainEvents);
    }
}
