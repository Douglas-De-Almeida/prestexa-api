using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class FormSetItemConfiguration : IEntityTypeConfiguration<FormSetItem>
    {
        public void Configure(EntityTypeBuilder<FormSetItem> builder)
        {
            builder.HasIndex(x => new { x.FormSetId, x.DisplayOrder });
            builder.HasIndex(x => new { x.FormSetId, x.FormDefinitionId }).IsUnique();

            builder.HasOne<FormSet>()
                .WithMany()
                .HasForeignKey(x => x.FormSetId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<FormDefinition>()
                .WithMany()
                .HasForeignKey(x => x.FormDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}