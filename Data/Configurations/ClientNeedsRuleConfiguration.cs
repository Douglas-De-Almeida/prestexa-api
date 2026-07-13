using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class ClientNeedsRuleConfiguration : IEntityTypeConfiguration<ClientNeedsRule>
    {
        public void Configure(EntityTypeBuilder<ClientNeedsRule> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Name }).IsUnique();
            builder.Property(x => x.ConditionJson).HasColumnType("jsonb");
            builder.Property(x => x.RequestedDocumentsJson).HasColumnType("jsonb");

            builder.HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}