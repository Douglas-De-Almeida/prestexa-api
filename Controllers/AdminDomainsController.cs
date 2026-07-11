using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/admin/domains")]
    [Authorize]
    public class AdminDomainsController : ControllerBase
    {
        private readonly IDomainSettingsService _domainSettingsService;

        public AdminDomainsController(IDomainSettingsService domainSettingsService)
        {
            _domainSettingsService = domainSettingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            try
            {
                var domain = await _domainSettingsService.GetCurrentAsync(cancellationToken);

                if (domain == null)
                {
                    return NotFound("Domain settings not found.");
                }

                return Ok(domain);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdateDomainSettingsRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var domain = await _domainSettingsService.UpsertCurrentAsync(request, cancellationToken);
                return Ok(domain);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(ex.ParamName ?? string.Empty, ex.Message);
                return ValidationProblem(ModelState);
            }
            catch (DomainConflictException)
            {
                return Conflict(new { error = "Subdomain already in use." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { error = "Subdomain already in use." });
            }
        }
    }
}
