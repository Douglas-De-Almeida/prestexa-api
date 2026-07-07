using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/companybranding")]
    public class CompanyBrandingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompanyBrandingController(AppDbContext context)
        {
            _context = context;
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
    }
}