using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data.Configurations;
using PrestexaAPI.Models;
using PrestexaAPI.Services;

namespace PrestexaAPI.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUser;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUser) : base(options)
        {
            _currentUser = currentUser;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<LoanDocument> LoanDocuments { get; set; }
        public DbSet<CompanyAsset> CompanyAssets { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<UserBranchRole> UserBranchRoles { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<BorrowerAddress> BorrowerAddresses { get; set; }
        public DbSet<MismoFile> MismoFiles { get; set; }
        public DbSet<LoginState> LoginStates { get; set; }
        public DbSet<TrustedMfaDevice> TrustedMfaDevices { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<PrestexaAPI.Models.MediaAsset> MediaAssets => Set<PrestexaAPI.Models.MediaAsset>();
        public DbSet<PrestexaAPI.Models.CompanyBranding> CompanyBrandings => Set<PrestexaAPI.Models.CompanyBranding>();
        public DbSet<LoanTerms> LoanTerms { get; set; }
        public DbSet<SubjectProperty> SubjectProperties { get; set; }
        public DbSet<BorrowerEmployment> BorrowerEmployments { get; set; }
        public DbSet<BorrowerIncome> BorrowerIncomes { get; set; }
        public DbSet<BorrowerAsset> BorrowerAssets { get; set; }
        public DbSet<BorrowerLiability> BorrowerLiabilities { get; set; }
        public DbSet<HousingExpense> HousingExpenses { get; set; }
        public DbSet<BorrowerDeclaration> BorrowerDeclarations { get; set; }
        public DbSet<GovernmentMonitoring> GovernmentMonitorings { get; set; }
        public DbSet<LoanMilestone> LoanMilestones { get; set; }
        public DbSet<LoanCondition> LoanConditions { get; set; }
        public DbSet<LoanTask> LoanTasks { get; set; }
        public DbSet<LoanOfficerAssignment> LoanOfficerAssignments { get; set; }
        public DbSet<Realtor> Realtors { get; set; }
        public DbSet<LoanRealtorAssignment> LoanRealtorAssignments { get; set; }
        public DbSet<MismoImportRecord> MismoImportRecords { get; set; }
        public DbSet<LoanActivity> LoanActivities { get; set; }
        public DbSet<LoanActivityAttachment> LoanActivityAttachments { get; set; }
        public DbSet<CreditReport> CreditReports { get; set; }
        public DbSet<OrganizationAuditRecord> OrganizationAuditRecords { get; set; }
        public DbSet<CompanyDomain> CompanyDomains { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // ✅ Company / Tenant
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.NmlsNumber)
                .IsUnique();

            modelBuilder.Entity<Company>()
                .HasAlternateKey(c => c.NmlsNumber);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.PosLoanAppAssigneeUser)
                .WithMany()
                .HasForeignKey(c => c.PosLoanAppAssigneeUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Company>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ✅ Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(u => u.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Loans
            modelBuilder.Entity<Loan>()
                .HasIndex(l => l.LoanNumber)
                .IsUnique();

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(l => l.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Loan Documents
            modelBuilder.Entity<LoanDocument>()
                .HasOne(d => d.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(d => d.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LoanDocument>()
                .HasOne(d => d.Loan)
                .WithMany()
                .HasForeignKey(d => d.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ MISMO Files
            modelBuilder.Entity<MismoFile>()
                .HasOne(m => m.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(m => m.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MismoFile>()
                .HasOne(m => m.Loan)
                .WithMany()
                .HasForeignKey(m => m.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Company Assets
            modelBuilder.Entity<CompanyAsset>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(a => a.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Roles
            modelBuilder.Entity<Role>()
                .HasIndex(r => new { r.CompanyNmlsNumber, r.Name })
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(r => r.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ Branches
            modelBuilder.Entity<Branch>()
                .HasIndex(b => new { b.CompanyNmlsNumber, b.Name })
                .IsUnique();

            modelBuilder.Entity<Branch>()
                .HasOne(b => b.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(b => b.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            // ✅ User Branch Roles
            modelBuilder.Entity<UserBranchRole>()
                .HasIndex(ubr => new { ubr.UserId, ubr.BranchId, ubr.RoleId })
                .IsUnique();

            modelBuilder.Entity<UserBranchRole>()
                .HasOne(ubr => ubr.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(ubr => ubr.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBranchRole>()
                .HasOne(ubr => ubr.User)
                .WithMany()
                .HasForeignKey(ubr => ubr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserBranchRole>()
                .HasOne(ubr => ubr.Branch)
                .WithMany()
                .HasForeignKey(ubr => ubr.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserBranchRole>()
                .HasOne(ubr => ubr.Role)
                .WithMany()
                .HasForeignKey(ubr => ubr.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Borrowers
            modelBuilder.Entity<Borrower>()
                .HasOne(b => b.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(b => b.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Borrower>()
                .HasOne(b => b.Loan)
                .WithMany()
                .HasForeignKey(b => b.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Borrower Addresses
            modelBuilder.Entity<BorrowerAddress>()
                .HasOne(a => a.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(a => a.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BorrowerAddress>()
                .HasOne(a => a.Borrower)
                .WithMany()
                .HasForeignKey(a => a.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Global tenant filters
            modelBuilder.Entity<Loan>()
                .HasQueryFilter(l =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     l.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanDocument>()
                .HasQueryFilter(d =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     d.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<MismoFile>()
                .HasQueryFilter(m =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     m.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<CompanyAsset>()
                .HasQueryFilter(a =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     a.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<Role>()
                .HasQueryFilter(r =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     r.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<Branch>()
                .HasQueryFilter(b =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     b.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<UserBranchRole>()
                .HasQueryFilter(ubr =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     ubr.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<Borrower>()
                .HasQueryFilter(b =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     b.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerAddress>()
                .HasQueryFilter(a =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     a.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            // ✅ Media Assets

            modelBuilder.Entity<PrestexaAPI.Models.MediaAsset>()
                .HasIndex(x => x.PublicId)
                .IsUnique();

            // ✅ Company Branding

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasIndex(x => x.CompanyNmlsNumber)
                .IsUnique();

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasOne<Company>()
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasOne(x => x.LightLogoAsset)
                .WithMany()
                .HasForeignKey(x => x.LightLogoAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasOne(x => x.DarkLogoAsset)
                .WithMany()
                .HasForeignKey(x => x.DarkLogoAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasOne(x => x.BackgroundAsset)
                .WithMany()
                .HasForeignKey(x => x.BackgroundAssetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrestexaAPI.Models.CompanyBranding>()
                .HasQueryFilter(b =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     b.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanTerms>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<SubjectProperty>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerEmployment>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerIncome>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerAsset>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerLiability>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<HousingExpense>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<BorrowerDeclaration>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<GovernmentMonitoring>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanMilestone>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanCondition>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanTask>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanOfficerAssignment>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<Realtor>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanRealtorAssignment>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<MismoImportRecord>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanActivity>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<LoanActivityAttachment>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<CreditReport>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));

            modelBuilder.Entity<OrganizationAuditRecord>()
                .HasQueryFilter(x =>
                    _currentUser.IsSuperAdmin ||
                    (_currentUser.CompanyNmlsNumber != null &&
                     x.CompanyNmlsNumber == _currentUser.CompanyNmlsNumber));
        }

        public override int SaveChanges()
        {
            ApplyCompanyCreationMetadata();
            EnforceImmutableCompanyNmls();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyCompanyCreationMetadata();
            EnforceImmutableCompanyNmls();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyCompanyCreationMetadata();
            EnforceImmutableCompanyNmls();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyCompanyCreationMetadata();
            EnforceImmutableCompanyNmls();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyCompanyCreationMetadata()
        {
            var addedCompanyEntries = ChangeTracker
                .Entries<Company>()
                .Where(entry => entry.State == EntityState.Added);

            foreach (var entry in addedCompanyEntries)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }

                if (!entry.Entity.CreatedByUserId.HasValue && _currentUser.UserId.HasValue)
                {
                    entry.Entity.CreatedByUserId = _currentUser.UserId.Value;
                }
            }
        }

        private void EnforceImmutableCompanyNmls()
        {
            var modifiedCompanyEntries = ChangeTracker
                .Entries<Company>()
                .Where(entry => entry.State == EntityState.Modified);

            foreach (var entry in modifiedCompanyEntries)
            {
                var originalNmls = entry.Property(x => x.NmlsNumber).OriginalValue;
                var currentNmls = entry.Property(x => x.NmlsNumber).CurrentValue;

                if (!string.Equals(originalNmls, currentNmls, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Company NMLS ID cannot be modified after organization creation.");
                }
            }
        }
    }
}