using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public MediaController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("asset/{publicId:guid}")]
        [HttpGet("/a/{publicId:guid}")]
        [ResponseCache(Duration = 31536000)]
        public async Task<IActionResult> GetAsset(Guid publicId)
        {
            var asset = await _context.MediaAssets
                .FirstOrDefaultAsync(x =>
                    x.PublicId == publicId &&
                    x.IsActive);

            if (asset == null)
                return NotFound();

            var storageRoot =
                _configuration["Storage:RootPath"];

            if (string.IsNullOrWhiteSpace(storageRoot))
            {
                storageRoot = Directory.Exists("/app/storage")
                    ? "/app/storage"
                    : Path.Combine(Directory.GetCurrentDirectory(), "storage");
            }

            var physicalPath =
                Path.Combine(storageRoot, asset.StoragePath);

            if (!System.IO.File.Exists(physicalPath))
                return NotFound();

            var stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return File(
                stream,
                asset.ContentType,
                enableRangeProcessing: true);
        }
    }
}