using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class FormSetConfiguration : IEntityTypeConfiguration<FormSet>
    {
        public void Configure(EntityTypeBuilder<FormSet> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Name }).IsUnique();

            builder.HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}