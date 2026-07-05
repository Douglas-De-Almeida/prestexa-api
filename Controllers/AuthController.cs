using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        private const string IssuerName = "Prestexa";
        private const int TwoFactorTemporaryTokenMinutes = 10;
        private const int EmployeeMfaRememberHours = 30;
        private const string EmployeePortal = "los";

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ============================================================
        // REGISTER - EMPLOYEE USER ONLY
        // ============================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.NmlsNumber == request.NmlsNumber && c.IsActive);

            if (company == null)
                return BadRequest("Invalid or inactive company.");

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (existingUser != null)
                return BadRequest("User already exists.");

            var user = new User
            {
                CompanyNmlsNumber = company.NmlsNumber,
                FirstName = request.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
                    ? null
                    : request.MiddleName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                Role = "User",
                Status = UserStatus.Active,
                TwoFactorEnabled = false,
                TwoFactorSecret = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully",
                user.Id,
                user.CompanyNmlsNumber,
                user.Email,
                user.Role,
                user.TwoFactorEnabled
            });
        }

        // ============================================================
        // START LOGIN TRANSACTION
        // ============================================================
        [HttpPost("start-login")]
        public async Task<IActionResult> StartLogin([FromBody] StartLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var portal = NormalizePortal(request.Portal);

            if (!IsAllowedPrestexaReturnUrl(
                    request.ReturnUrl,
                    portal,
                    out var parsedUri,
                    out var tenantSlug))
            {
                return BadRequest("Invalid return URL.");
            }

            var stateId = Guid.NewGuid();

            var loginState = new LoginState
            {
                StateId = stateId,
                ReturnUrl = parsedUri.ToString(),
                Portal = portal,
                SourceHost = parsedUri.Host.ToLowerInvariant(),
                TenantSlug = tenantSlug,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            };

            _context.LoginStates.Add(loginState);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                state = stateId,
                portal,
                loginUrl = BuildLoginUrl(portal, stateId)
            });
        }

        // ============================================================
        // LOGIN CONTEXT / BRANDING
        // ============================================================
        [HttpGet("login-context")]
        public async Task<IActionResult> LoginContext([FromQuery] string state)
        {
            if (!Guid.TryParse(state, out var stateId))
                return BadRequest("Invalid state.");

            var loginState = await _context.LoginStates
                .FirstOrDefaultAsync(x => x.StateId == stateId);

            if (loginState == null)
                return NotFound("Login state not found.");

            if (loginState.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Login state expired.");

            if (loginState.IsUsed)
                return BadRequest("Login state already used.");

            return Ok(new
            {
                state = loginState.StateId,
                portal = loginState.Portal,
                returnUrl = loginState.ReturnUrl,
                sourceHost = loginState.SourceHost,
                tenantSlug = loginState.TenantSlug,
                branding = new
                {
                    companyName = string.IsNullOrWhiteSpace(loginState.TenantSlug)
                        ? "Prestexa"
                        : loginState.TenantSlug,
                    logoUrl = "",
                    primaryColor = "#1d4ce9"
                }
            });
        }

        // ============================================================
        // CONSUME LOGIN STATE
        // ============================================================
        [Authorize]
        [HttpPost("consume-login-state")]
        public async Task<IActionResult> ConsumeLoginState(
            [FromBody] ConsumeLoginStateRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!Guid.TryParse(request.State, out var stateId))
                return BadRequest("Invalid state.");

            var loginState = await _context.LoginStates
                .FirstOrDefaultAsync(x => x.StateId == stateId);

            if (loginState == null)
                return NotFound("Login state not found.");

            if (loginState.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Login state expired.");

            if (loginState.IsUsed)
                return BadRequest("Login state already used.");

            if (!IsAllowedPrestexaReturnUrl(
                    loginState.ReturnUrl,
                    loginState.Portal,
                    out var parsedUri,
                    out _))
            {
                return BadRequest("Invalid return URL.");
            }

            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                loginState.UsedByUserId = userId;
            }

            loginState.IsUsed = true;
            loginState.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                portal = loginState.Portal,
                returnUrl = parsedUri.ToString()
            });
        }

        // ============================================================
        // LOGIN - EMPLOYEE / LOS FLOW
        // ============================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            if (user.Company == null || !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (!RequiresEmployeeAuthenticator(user))
            {
                var token = CreateFinalJwtToken(user);

                return Ok(new
                {
                    token,
                    user = BuildUserResponse(user)
                });
            }

            if (!user.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                {
                    user.TwoFactorSecret = GenerateBase32Secret();
                    await _context.SaveChangesAsync();
                }

                var setupToken = CreateTwoFactorTemporaryToken(user, "2fa_setup");
                var otpauthUri = BuildOtpAuthUri(user.Email, user.TwoFactorSecret);

                return Ok(new
                {
                    requiresAuthenticatorSetup = true,
                    setupToken,
                    email = user.Email,
                    manualKey = user.TwoFactorSecret,
                    otpauthUri,
                    message = "Authenticator setup is required."
                });
            }

            var hasTrustedDevice = await HasValidTrustedMfaDeviceAsync(
                user,
                request.DeviceId
            );

            if (hasTrustedDevice)
            {
                var finalToken = CreateFinalJwtToken(user);

                return Ok(new
                {
                    token = finalToken,
                    user = BuildUserResponse(user),
                    mfaRemembered = true,
                    message = "Login successful. MFA was remembered for this device."
                });
            }

            var twoFactorToken = CreateTwoFactorTemporaryToken(user, "2fa_login");

            return Ok(new
            {
                requiresTwoFactor = true,
                twoFactorToken,
                email = user.Email,
                message = "Two-factor authentication is required."
            });
        }

        // ============================================================
        // ENABLE AUTHENTICATOR AFTER FIRST-TIME SETUP
        // ============================================================
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableAuthenticator(
            [FromBody] EnableAuthenticatorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = ValidateTwoFactorTemporaryToken(
                request.SetupToken,
                "2fa_setup"
            );

            if (principal == null)
                return Unauthorized("Invalid or expired setup token.");

            var userIdValue = principal.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized("Invalid setup token.");

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (user.Company == null || !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                return BadRequest("Authenticator setup has not been started.");

            var isValidCode = VerifyTotpCode(user.TwoFactorSecret, request.Code);

            if (!isValidCode)
                return BadRequest("Invalid authenticator code.");

            user.TwoFactorEnabled = true;
            user.TwoFactorEnabledAt = DateTime.UtcNow;
            user.TwoFactorLastVerifiedAt = DateTime.UtcNow;

            await RememberTrustedMfaDeviceAsync(user, request.DeviceId);

            await _context.SaveChangesAsync();

            var finalToken = CreateFinalJwtToken(user);

            return Ok(new
            {
                token = finalToken,
                user = BuildUserResponse(user),
                message = "Authenticator enabled successfully."
            });
        }

        // ============================================================
        // VERIFY AUTHENTICATOR FOR FUTURE LOGINS
        // ============================================================
        [HttpPost("2fa/verify-login")]
        public async Task<IActionResult> VerifyTwoFactorLogin(
            [FromBody] VerifyTwoFactorLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = ValidateTwoFactorTemporaryToken(
                request.TwoFactorToken,
                "2fa_login"
            );

            if (principal == null)
                return Unauthorized("Invalid or expired two-factor token.");

            var userIdValue = principal.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized("Invalid two-factor token.");

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (user.Company == null || !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                return BadRequest("Authenticator is not enabled for this user.");

            var isValidCode = VerifyTotpCode(user.TwoFactorSecret, request.Code);

            if (!isValidCode)
                return BadRequest("Invalid authenticator code.");

            user.TwoFactorLastVerifiedAt = DateTime.UtcNow;

            await RememberTrustedMfaDeviceAsync(user, request.DeviceId);

            await _context.SaveChangesAsync();

            var finalToken = CreateFinalJwtToken(user);

            return Ok(new
            {
                token = finalToken,
                user = BuildUserResponse(user),
                message = "Two-factor authentication successful."
            });
        }

        // ============================================================
        // PORTAL / RETURN URL HELPERS
        // ============================================================
        private static string NormalizePortal(string? portal)
        {
            var normalized = portal?.Trim().ToLowerInvariant();

            return normalized switch
            {
                "borrower" => "borrower",
                "rea" => "rea",
                "los" => "los",
                _ => "los"
            };
        }

        private static string BuildLoginUrl(string portal, Guid stateId)
        {
            return portal switch
            {
                "borrower" => $"https://auth.prestexa.com/account/login?state={stateId}",
                "rea" => $"https://auth.prestexa.com/crm/login?state={stateId}",
                _ => $"https://auth.prestexa.com/login?state={stateId}"
            };
        }

        private static bool IsAllowedPrestexaReturnUrl(
            string returnUrl,
            string portal,
            out Uri parsedUri,
            out string? tenantSlug)
        {
            parsedUri = null!;
            tenantSlug = null;

            if (string.IsNullOrWhiteSpace(returnUrl))
                return false;

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                return false;

            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return false;

            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath.ToLowerInvariant();

            if (!host.EndsWith(".prestexa.com", StringComparison.OrdinalIgnoreCase))
                return false;

            var reservedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "api.prestexa.com",
                "auth.prestexa.com",
                "www.prestexa.com",
                "prestexa.com"
            };

            if (reservedHosts.Contains(host))
                return false;

            if (host == "los.prestexa.com")
            {
                if (portal == "borrower" && !path.StartsWith("/account"))
                    return false;

                if (portal == "rea" && !path.StartsWith("/crm"))
                    return false;

                if (portal == "los" &&
                    (path.StartsWith("/account") || path.StartsWith("/crm")))
                    return false;

                parsedUri = uri;
                return true;
            }

            var slug = host.Replace(".prestexa.com", "");

            if (string.IsNullOrWhiteSpace(slug))
                return false;

            if (!IsSafeTenantSlug(slug))
                return false;

            if (portal == "borrower" && !path.StartsWith("/account"))
                return false;

            if (portal == "rea" && !path.StartsWith("/crm"))
                return false;

            if (portal == "los" &&
                (path.StartsWith("/account") || path.StartsWith("/crm")))
                return false;

            tenantSlug = slug;
            parsedUri = uri;

            return true;
        }

        private static bool IsSafeTenantSlug(string slug)
        {
            if (slug.Length > 100)
                return false;

            return slug.All(character =>
                char.IsLetterOrDigit(character) ||
                character == '-');
        }

        // ============================================================
        // DEVICE ID / TRUSTED MFA DEVICE HELPERS
        // ============================================================
        private static string? NormalizeDeviceId(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return null;

            var normalized = deviceId.Trim();

            if (normalized.Length > 100)
                return null;

            return normalized;
        }

        private async Task<bool> HasValidTrustedMfaDeviceAsync(
            User user,
            string? deviceId)
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);

            if (normalizedDeviceId == null)
                return false;

            var trustedDevice = await _context.TrustedMfaDevices
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == EmployeePortal);

            if (trustedDevice == null)
                return false;

            return trustedDevice.ExpiresAt > DateTime.UtcNow;
        }

        private async Task RememberTrustedMfaDeviceAsync(
            User user,
            string? deviceId)
        {
            var normalizedDeviceId = NormalizeDeviceId(deviceId);

            if (normalizedDeviceId == null)
                return;

            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(EmployeeMfaRememberHours);

            var trustedDevice = await _context.TrustedMfaDevices
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.DeviceId == normalizedDeviceId &&
                    x.Portal == EmployeePortal);

            if (trustedDevice == null)
            {
                trustedDevice = new TrustedMfaDevice
                {
                    UserId = user.Id,
                    DeviceId = normalizedDeviceId,
                    Portal = EmployeePortal,
                    LastMfaAt = now,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                };

                _context.TrustedMfaDevices.Add(trustedDevice);
            }
            else
            {
                trustedDevice.LastMfaAt = now;
                trustedDevice.ExpiresAt = expiresAt;
                trustedDevice.UpdatedAt = now;
            }
        }

        // ============================================================
        // JWT / MFA HELPERS
        // ============================================================
        private string CreateFinalJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim("CompanyNmlsNumber", user.CompanyNmlsNumber),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Portal", EmployeePortal)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string CreateTwoFactorTemporaryToken(User user, string purpose)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("Email", user.Email),
                new Claim("CompanyNmlsNumber", user.CompanyNmlsNumber),
                new Claim("Purpose", purpose)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(TwoFactorTemporaryTokenMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidateTwoFactorTemporaryToken(
            string token,
            string expectedPurpose)
        {
            try
            {
                var jwtKey = _config["Jwt:Key"];

                if (string.IsNullOrWhiteSpace(jwtKey))
                    throw new Exception("JWT Key missing.");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var principal = tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    },
                    out _
                );

                var purpose = principal.FindFirst("Purpose")?.Value;

                if (!string.Equals(purpose, expectedPurpose, StringComparison.Ordinal))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateBase32Secret()
        {
            var secretBytes = RandomNumberGenerator.GetBytes(20);
            return Base32Encoding.ToString(secretBytes);
        }

        private static bool VerifyTotpCode(string base32Secret, string code)
        {
            if (string.IsNullOrWhiteSpace(base32Secret))
                return false;

            if (string.IsNullOrWhiteSpace(code))
                return false;

            var cleanCode = new string(code.Where(char.IsDigit).ToArray());

            if (cleanCode.Length != 6)
                return false;

            var secretBytes = Base32Encoding.ToBytes(base32Secret);

            var totp = new Totp(
                secretBytes,
                step: 30,
                mode: OtpHashMode.Sha1,
                totpSize: 6
            );

            return totp.VerifyTotp(
                cleanCode,
                out _,
                new VerificationWindow(previous: 1, future: 1)
            );
        }

        private static string BuildOtpAuthUri(string email, string base32Secret)
        {
            var issuer = Uri.EscapeDataString(IssuerName);
            var account = Uri.EscapeDataString(email);
            var label = $"{issuer}:{account}";

            return
                $"otpauth://totp/{label}" +
                $"?secret={base32Secret}" +
                $"&issuer={issuer}" +
                $"&algorithm=SHA1" +
                $"&digits=6" +
                $"&period=30";
        }

        private static bool RequiresEmployeeAuthenticator(User user)
        {
            return !string.Equals(user.Role, "Borrower", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.Role, "REA", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.Role, "RealEstateAgent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(user.Role, "Agent", StringComparison.OrdinalIgnoreCase);
        }

        private static object BuildUserResponse(User user)
        {
            return new
            {
                user.Id,
                user.Email,
                user.Role,
                user.CompanyNmlsNumber,
                CompanyName = user.Company?.Name,
                user.TwoFactorEnabled
            };
        }
    }

    public class EnableAuthenticatorRequest
    {
        public string SetupToken { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? DeviceId { get; set; }
    }

    public class VerifyTwoFactorLoginRequest
    {
        public string TwoFactorToken { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? DeviceId { get; set; }
    }
}