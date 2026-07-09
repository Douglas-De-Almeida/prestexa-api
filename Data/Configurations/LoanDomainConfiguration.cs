using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class LoanTermsConfiguration : IEntityTypeConfiguration<LoanTerms>
    {
        public void Configure(EntityTypeBuilder<LoanTerms> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId }).IsUnique();

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class SubjectPropertyConfiguration : IEntityTypeConfiguration<SubjectProperty>
    {
        public void Configure(EntityTypeBuilder<SubjectProperty> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId }).IsUnique();

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LoanMilestoneConfiguration : IEntityTypeConfiguration<LoanMilestone>
    {
        public void Configure(EntityTypeBuilder<LoanMilestone> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.MilestoneType });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LoanConditionConfiguration : IEntityTypeConfiguration<LoanCondition>
    {
        public void Configure(EntityTypeBuilder<LoanCondition> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.ConditionStatus });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LoanTaskConfiguration : IEntityTypeConfiguration<LoanTask>
    {
        public void Configure(EntityTypeBuilder<LoanTask> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.TaskStatus });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.LoanCondition)
                .WithMany()
                .HasForeignKey(x => x.LoanConditionId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.AssignedUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class LoanOfficerAssignmentConfiguration : IEntityTypeConfiguration<LoanOfficerAssignment>
    {
        public void Configure(EntityTypeBuilder<LoanOfficerAssignment> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.UserId }).IsUnique();

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class RealtorConfiguration : IEntityTypeConfiguration<Realtor>
    {
        public void Configure(EntityTypeBuilder<Realtor> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.Email });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class LoanRealtorAssignmentConfiguration : IEntityTypeConfiguration<LoanRealtorAssignment>
    {
        public void Configure(EntityTypeBuilder<LoanRealtorAssignment> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.RealtorId }).IsUnique();

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Realtor)
                .WithMany()
                .HasForeignKey(x => x.RealtorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
