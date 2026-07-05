using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PrestexaAPI.Services;

namespace PrestexaAPI.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var connectionString =
                Environment.GetEnvironmentVariable("DESIGNTIME_CONNECTION_STRING")
                ?? "Host=prestexa-db;Database=prestexa_db;Username=prestexa_admin;Password=CHANGE_ME";

            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(
                optionsBuilder.Options,
                new DesignTimeCurrentUserService()
            );
        }
    }

    public class DesignTimeCurrentUserService : ICurrentUserService
    {
        public int? UserId => null;
        public string? CompanyNmlsNumber => null;
        public string? Role => "SuperAdmin";
        public bool IsSuperAdmin => true;
    }
}