using FlowBoard.Domain.Entities;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class BoardListConfiguration : IEntityTypeConfiguration<BoardList>
{
    public void Configure(EntityTypeBuilder<BoardList> builder)
    {
        builder.ToTable("board_lists");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.BoardId).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();

        builder.Property(l => l.Position)
            .HasConversion(
                position => position.Value,
                value => FractionalIndex.FromTrustedSource(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.UpdatedAt).IsRequired();

        // Ordering reads always scope by board then sort by position
        builder.HasIndex(l => new { l.BoardId, l.Position });

        builder.HasOne<Board>()
            .WithMany()
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => !l.IsDeleted);

        builder.Ignore(l => l.DomainEvents);
    }
}
