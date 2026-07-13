using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class ClosingCostItemConfiguration : IEntityTypeConfiguration<ClosingCostItem>
    {
        public void Configure(EntityTypeBuilder<ClosingCostItem> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.DisplayOrder });
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.FeeName }).IsUnique();
            builder.Property(x => x.Amount).HasPrecision(18, 2);
            builder.Property(x => x.Percentage).HasPrecision(9, 6);

            builder.HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}