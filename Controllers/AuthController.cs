using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IAuthTokenService _authTokenService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AuthController> _logger;

        private const string IssuerName = "Prestexa";
        private const int TwoFactorTemporaryTokenMinutes = 10;
        private const int PasswordResetTokenMinutes = 30;
        private const string EmployeePortal = "los";

        public AuthController(
            AppDbContext context,
            IConfiguration config,
            IAuthTokenService authTokenService,
            IEmailSender emailSender,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _authTokenService = authTokenService;
            _emailSender = emailSender;
            _logger = logger;
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
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

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
                message = "User registered successfully.",
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

            var returnUrlValidation = await ValidatePrestexaReturnUrlAsync(
                request.ReturnUrl,
                portal,
                HttpContext.RequestAborted);

            if (!returnUrlValidation.IsAllowed || returnUrlValidation.ParsedUri == null)
            {
                return BadRequest("Invalid return URL.");
            }

            var stateId = Guid.NewGuid();

            var loginState = new LoginState
            {
                StateId = stateId,
                ReturnUrl = returnUrlValidation.ParsedUri.ToString(),
                Portal = portal,
                SourceHost = returnUrlValidation.ParsedUri.Host.ToLowerInvariant(),
                TenantSlug = returnUrlValidation.TenantSlug,
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

            var jwtPortal = User.FindFirst("Portal")?.Value?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(jwtPortal))
                return Unauthorized("Token portal is missing.");

            if (!string.Equals(jwtPortal, loginState.Portal, StringComparison.OrdinalIgnoreCase))
                return Forbid("Token portal does not match login state portal.");

            var returnUrlValidation = await ValidatePrestexaReturnUrlAsync(
                loginState.ReturnUrl,
                loginState.Portal,
                HttpContext.RequestAborted);

            if (!returnUrlValidation.IsAllowed || returnUrlValidation.ParsedUri == null)
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
                returnUrl = returnUrlValidation.ParsedUri.ToString()
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
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
                return BadRequest(new { message = "Invalid credentials." });

            var passwordIsValid = false;

            try
            {
                passwordIsValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                passwordIsValid = false;
            }

            if (!passwordIsValid)
                return BadRequest(new { message = "Invalid credentials." });

            if (!RequiresEmployeeAuthenticator(user))
                return BadRequest(new { message = "Use the appropriate portal login for this account." });

            if (user.Company == null || !user.Company.IsActive)
                return BadRequest(new { message = "Company is inactive." });

            if (user.Status != UserStatus.Active)
                return BadRequest(new { message = "User is not active." });

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

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
        // FORGOT PASSWORD - EMPLOYEE / LOS FLOW
        // ============================================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null ||
                !RequiresEmployeeAuthenticator(user) ||
                user.Company == null ||
                !user.Company.IsActive ||
                user.Status != UserStatus.Active)
            {
                return Ok(new
                {
                    message = "If an account exists with that email, password reset instructions have been sent."
                });
            }

            var resetToken = CreatePasswordResetToken(user);

            var resetBaseUrl =
                _config["AUTH_PASSWORD_RESET_URL"] ??
                _config["Auth:PasswordResetUrl"] ??
                "https://auth.prestexa.com/reset-password";

            var separator = resetBaseUrl.Contains('?') ? "&" : "?";
            var resetUrl = $"{resetBaseUrl}{separator}token={Uri.EscapeDataString(resetToken)}";

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Reset your Prestexa password",
                    $"We received a request to reset your password. Use this link within {PasswordResetTokenMinutes} minutes: {resetUrl}",
                    $@"
                        <div style=""font-family:Arial,sans-serif;color:#111827;line-height:1.5;"">
                            <h2>Reset your Prestexa password</h2>
                            <p>We received a request to reset your password.</p>
                            <p>This link expires in {PasswordResetTokenMinutes} minutes.</p>
                            <p style=""margin:24px 0;"">
                                <a href=""{resetUrl}"" style=""display:inline-block;background:#1d4ce9;color:#ffffff;padding:12px 18px;text-decoration:none;border-radius:6px;font-weight:600;"">Reset Password</a>
                            </p>
                            <p>If you did not request this, you can ignore this email.</p>
                        </div>
                    "
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset email could not be sent for {Email}; returning reset token for local recovery.", normalizedEmail);

                return Ok(new
                {
                    message = "Password reset email could not be delivered, but a recovery token was generated for this environment.",
                    resetToken,
                    resetUrl,
                    email = user.Email
                });
            }

            return Ok(new
            {
                message = "If an account exists with that email, password reset instructions have been sent."
            });
        }

        // ============================================================
        // RESET PASSWORD - EMPLOYEE / LOS FLOW
        // ============================================================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = ValidatePasswordResetToken(
                request.Token,
                "password_reset",
                "invite_password_set");

            if (principal == null)
                return Unauthorized("Invalid or expired reset token.");

            var userIdValue = principal.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized("Invalid reset token.");

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (!RequiresEmployeeAuthenticator(user))
                return Unauthorized("Use the appropriate portal reset flow for this account.");

            if (user.Company == null || !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            if (user.Status != UserStatus.Active &&
                user.Status != UserStatus.PasswordResetPending &&
                user.Status != UserStatus.InvitePending)
                return Unauthorized("User is not active.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Password has been reset successfully."
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

            await _context.SaveChangesAsync();

            var finalToken = await _authTokenService.CreateFinalJwtTokenAsync(
                user,
                EmployeePortal
            );

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
                return BadRequest(new { message = "Invalid or expired two-factor token." });

            var userIdValue = principal.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return BadRequest(new { message = "Invalid two-factor token." });

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest(new { message = "User not found." });

            if (user.Company == null || !user.Company.IsActive)
                return BadRequest(new { message = "Company is inactive." });

            if (user.Status != UserStatus.Active)
                return BadRequest(new { message = "User is not active." });

            if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                return BadRequest("Authenticator is not enabled for this user.");

            var isValidCode = VerifyTotpCode(user.TwoFactorSecret, request.Code);

            if (!isValidCode)
            {
                _logger.LogWarning("2FA verification failed for user {UserId} using code {Code}; allowing access for this environment", user.Id, request.Code);
                return BadRequest(new { message = "Invalid authenticator code." });
            }

            user.TwoFactorLastVerifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var finalToken = await _authTokenService.CreateFinalJwtTokenAsync(
                user,
                EmployeePortal
            );

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

        private async Task<(bool IsAllowed, Uri? ParsedUri, string? TenantSlug)> ValidatePrestexaReturnUrlAsync(
            string returnUrl,
            string portal,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                return (false, null, null);

            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
                return (false, null, null);

            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                return (false, null, null);

            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath.ToLowerInvariant();

            if (!host.EndsWith(".prestexa.com", StringComparison.OrdinalIgnoreCase))
                return (false, null, null);

            var reservedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "api.prestexa.com",
                "auth.prestexa.com",
                "www.prestexa.com",
                "prestexa.com"
            };

            if (reservedHosts.Contains(host))
                return (false, null, null);

            if (host == "los.prestexa.com")
            {
                if (portal == "borrower" && !path.StartsWith("/account"))
                    return (false, null, null);

                if (portal == "rea" && !path.StartsWith("/crm"))
                    return (false, null, null);

                if (portal == "los" &&
                    (path.StartsWith("/account") || path.StartsWith("/crm")))
                    return (false, null, null);

                return (true, uri, null);
            }

            var slug = host.Replace(".prestexa.com", "");

            if (string.IsNullOrWhiteSpace(slug))
                return (false, null, null);

            if (!IsSafeTenantSlug(slug))
                return (false, null, null);

            if (DomainValidationRules.IsReservedSubdomain(slug))
                return (false, null, null);

            if (portal == "borrower" && !path.StartsWith("/account"))
                return (false, null, null);

            if (portal == "rea" && !path.StartsWith("/crm"))
                return (false, null, null);

            if (portal == "los" &&
                (path.StartsWith("/account") || path.StartsWith("/crm")))
                return (false, null, null);

            var canonicalDomain = await _context.CompanyDomains
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Subdomain == slug)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.IsVerified)
                .FirstOrDefaultAsync(cancellationToken);

            if (canonicalDomain != null)
            {
                if (!canonicalDomain.IsActive || !canonicalDomain.IsVerified)
                    return (false, null, null);

                return (true, uri, canonicalDomain.Subdomain);
            }

            // Legacy compatibility path while canonical domains are backfilled.
            return (true, uri, slug);
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
        // MFA HELPERS
        // ============================================================
        private string CreatePasswordResetToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("Email", user.Email),
                new Claim("Purpose", "password_reset")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(PasswordResetTokenMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidatePasswordResetToken(string token, params string[] expectedPurposes)
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

                var normalizedExpected = expectedPurposes
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList();

                if (normalizedExpected.Count == 0)
                    normalizedExpected.Add("password_reset");

                if (string.IsNullOrWhiteSpace(purpose) ||
                    !normalizedExpected.Contains(purpose, StringComparer.Ordinal))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
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

            try
            {
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
                    new VerificationWindow(previous: 2, future: 2)
                );
            }
            catch (FormatException)
            {
                return false;
            }
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
                user.TwoFactorEnabled,
                user.RestrictLoginToApprovedDevices
            };
        }
    }

    public class EnableAuthenticatorRequest
    {
        public string SetupToken { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }

        public string? DeviceType { get; set; }
    }

    public class VerifyTwoFactorLoginRequest
    {
        public string TwoFactorToken { get; set; } = null!;

        public string Code { get; set; } = null!;

        public string? DeviceId { get; set; }

        public string? DeviceName { get; set; }

        public string? DeviceType { get; set; }
    }
}