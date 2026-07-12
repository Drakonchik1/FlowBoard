using FlowBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowBoard.Infrastructure.Persistence.Configurations;

internal sealed class CardTagConfiguration : IEntityTypeConfiguration<CardTag>
{
    public void Configure(EntityTypeBuilder<CardTag> builder)
    {
        builder.ToTable("card_tags");

        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Id).ValueGeneratedNever();

        builder.Property(ct => ct.CardId).IsRequired();
        builder.Property(ct => ct.TagId).IsRequired();
        builder.Property(ct => ct.CreatedAt).IsRequired();

        builder.HasIndex(ct => new { ct.CardId, ct.TagId }).IsUnique();
        builder.HasIndex(ct => ct.TagId);

        builder.HasOne<Card>()
            .WithMany()
            .HasForeignKey(ct => ct.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(ct => ct.DomainEvents);
    }
}
