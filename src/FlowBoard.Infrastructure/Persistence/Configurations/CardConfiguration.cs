using FlowBoard.Domain.Entities;
using FlowBoard.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.BoardListId).IsRequired();
        builder.Property(c => c.BoardId).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(4000);

        builder.Property(c => c.Position)
            .HasConversion(
                position => position.Value,
                value => FractionalIndex.FromTrustedSource(value))
            .HasMaxLength(100)
            .IsRequired();

        // Enum stored as string — adding new priorities never reorders existing rows
        builder.Property(c => c.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsConcurrencyToken().IsRequired();

        // Per-list ordering reads, and board-wide reads for the Dapper GetBoard query.
        // Unique among active cards so concurrent moves cannot persist duplicate positions.
        builder.HasIndex(c => new { c.BoardListId, c.Position })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(c => c.BoardId);

        builder.HasOne<BoardList>()
            .WithMany()
            .HasForeignKey(c => c.BoardListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Ignore(c => c.DomainEvents);
    }
}
