using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class CompanyDomainConfiguration : IEntityTypeConfiguration<CompanyDomain>
    {
        public void Configure(EntityTypeBuilder<CompanyDomain> builder)
        {
            builder.HasIndex(x => x.Subdomain)
                .IsUnique();

            builder.HasIndex(x => new { x.CompanyId, x.IsActive });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
