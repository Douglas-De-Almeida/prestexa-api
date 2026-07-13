using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
    {
        public void Configure(EntityTypeBuilder<FormDefinition> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Name }).IsUnique();
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Category });

            builder.HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<OperationalAsset>()
                .WithMany()
                .HasForeignKey(x => x.OperationalAssetId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}