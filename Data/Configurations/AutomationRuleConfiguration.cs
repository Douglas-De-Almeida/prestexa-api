using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
    {
        public void Configure(EntityTypeBuilder<AutomationRule> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Name }).IsUnique();
            builder.Property(x => x.TriggerJson).HasColumnType("jsonb");
            builder.Property(x => x.ActionJson).HasColumnType("jsonb");

            builder.HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}