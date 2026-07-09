using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/companybranding")]
    public class CompanyBrandingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private const long MaxUploadBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/png",
                "image/jpeg",
                "image/jpg",
                "image/webp",
                "image/svg+xml"
            };

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".webp",
                ".svg"
            };

        public CompanyBrandingController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("{nmls}")]
        public async Task<IActionResult> GetBranding(
            string nmls)
        {
            var branding =
                await _context.CompanyBrandings
                .Include(x => x.LightLogoAsset)
                .Include(x => x.DarkLogoAsset)
                .Include(x => x.BackgroundAsset)
                .FirstOrDefaultAsync(x =>
                    x.CompanyNmlsNumber == nmls &&
                    x.IsActive);

            if (branding == null)
            {
                return Ok(new
                {
                    companyName = "Prestexa",
                    primaryColor = "#1d4ce9",
                    secondaryColor = "#ffffff",
                    accentColor = "#2563eb",
                    lightLogoUrl = (string?)null,
                    darkLogoUrl = (string?)null,
                    backgroundUrl = (string?)null
                });
            }

            return Ok(new
            {
                companyName = branding.CompanyName,

                branding.PrimaryColor,

                branding.SecondaryColor,

                branding.AccentColor,

                lightLogoUrl =
                    branding.LightLogoAsset == null
                        ? null
                        : $"https://media.prestexa.com/a/{branding.LightLogoAsset.PublicId}",

                darkLogoUrl =
                    branding.DarkLogoAsset == null
                        ? null
                        : $"https://media.prestexa.com/a/{branding.DarkLogoAsset.PublicId}",

                backgroundUrl =
                    branding.BackgroundAsset == null
                        ? null
                        : $"https://media.prestexa.com/a/{branding.BackgroundAsset.PublicId}"
            });
        }

        [Authorize]
        [HttpPost("{nmls}/light-logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLightLogo(string nmls, IFormFile file)
        {
            var result = await SaveBrandingAssetAsync(nmls, file, BrandingAssetType.LightLogo);
            return result;
        }

        [Authorize]
        [HttpPost("{nmls}/dark-logo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDarkLogo(string nmls, IFormFile file)
        {
            var result = await SaveBrandingAssetAsync(nmls, file, BrandingAssetType.DarkLogo);
            return result;
        }

        [Authorize]
        [HttpPost("{nmls}/background")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBackground(string nmls, IFormFile file)
        {
            var result = await SaveBrandingAssetAsync(nmls, file, BrandingAssetType.Background);
            return result;
        }

        private async Task<IActionResult> SaveBrandingAssetAsync(
            string nmls,
            IFormFile file,
            BrandingAssetType assetType)
        {
            if (string.IsNullOrWhiteSpace(nmls))
                return BadRequest("NMLS is required.");

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > MaxUploadBytes)
                return BadRequest("Max upload size is 10MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                return BadRequest("Invalid file extension.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                return BadRequest("Invalid file content type.");

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.NmlsNumber == nmls);

            if (company == null)
                return NotFound("Company not found.");

            var storageRoot = _configuration["Storage:RootPath"];

            if (string.IsNullOrWhiteSpace(storageRoot))
            {
                storageRoot = Directory.Exists("/app/storage")
                    ? "/app/storage"
                    : Path.Combine(Directory.GetCurrentDirectory(), "storage");
            }

            var normalizedNmls = new string(nmls.Where(char.IsLetterOrDigit).ToArray());

            if (string.IsNullOrWhiteSpace(normalizedNmls))
                return BadRequest("Invalid NMLS format.");

            var relativeFolder = Path.Combine("companies", normalizedNmls, "branding");
            var physicalFolder = Path.Combine(storageRoot, relativeFolder);

            Directory.CreateDirectory(physicalFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(physicalFolder, storedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var mediaAsset = new MediaAsset
            {
                StoragePath = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/'),
                ContentType = file.ContentType,
                FileName = Path.GetFileName(file.FileName),
                FileSizeBytes = file.Length,
                UploadedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.MediaAssets.Add(mediaAsset);

            var branding = await _context.CompanyBrandings
                .FirstOrDefaultAsync(x => x.CompanyNmlsNumber == nmls);

            if (branding == null)
            {
                branding = new CompanyBranding
                {
                    CompanyNmlsNumber = nmls,
                    CompanyName = company.Name,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.CompanyBrandings.Add(branding);
            }

            switch (assetType)
            {
                case BrandingAssetType.LightLogo:
                    branding.LightLogoAsset = mediaAsset;
                    break;
                case BrandingAssetType.DarkLogo:
                    branding.DarkLogoAsset = mediaAsset;
                    break;
                default:
                    branding.BackgroundAsset = mediaAsset;
                    break;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Branding asset uploaded successfully.",
                nmls,
                assetType = assetType.ToString(),
                publicId = mediaAsset.PublicId,
                url = $"https://media.prestexa.com/a/{mediaAsset.PublicId}"
            });
        }

        private enum BrandingAssetType
        {
            LightLogo,
            DarkLogo,
            Background
        }
    }
}