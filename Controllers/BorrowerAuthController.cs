using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
    public class BorrowerAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailSender _emailSender;
        private readonly IAuthTokenService _authTokenService;
        private readonly ITrustedDeviceService _trustedDeviceService;
        private readonly ILogger<BorrowerAuthController> _logger;

        private const int OtpTokenMinutes = 10;
        private const int PasswordResetTokenMinutes = 30;
        private const string BorrowerPortal = "borrower";
        private static readonly TimeSpan BorrowerRememberDuration = TimeSpan.FromDays(7);

        public BorrowerAuthController(
            AppDbContext context,
            IConfiguration config,
            IEmailSender emailSender,
            IAuthTokenService authTokenService,
            ITrustedDeviceService trustedDeviceService,
            ILogger<BorrowerAuthController> logger)
        {
            _context = context;
            _config = config;
            _emailSender = emailSender;
            _authTokenService = authTokenService;
            _trustedDeviceService = trustedDeviceService;
            _logger = logger;
        }

        // ============================================================
        // REGISTER BORROWER
        // ============================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PortalRegisterRequest request)
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
                Role = "Borrower",
                Status = UserStatus.Active,
                TwoFactorEnabled = false,
                TwoFactorSecret = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Borrower registered successfully.",
                user.Id,
                user.Email,
                user.Role,
                user.CompanyNmlsNumber,
                CompanyName = company.Name
            });
        }

        // ============================================================
        // LOGIN BORROWER
        // ============================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] PortalLoginRequest request)
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

            if (!IsBorrower(user))
                return Unauthorized("This account is not a borrower account.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            if (user.Company != null && !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var isTrustedDevice = await _trustedDeviceService.IsTrustedAsync(
                user.Id,
                BorrowerPortal,
                request.DeviceId
            );

            if (isTrustedDevice)
            {
                var finalToken = await _authTokenService.CreateFinalJwtTokenAsync(
                    user,
                    BorrowerPortal
                );

                return Ok(new
                {
                    token = finalToken,
                    user = BuildUserResponse(user),
                    mfaRemembered = true,
                    message = "Borrower login successful. Verification was remembered for this device."
                });
            }

            var code = GenerateSixDigitCode();
            var otpToken = CreateOtpTemporaryToken(user, "borrower_otp", code);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Your Prestexa verification code",
                $"Your Prestexa verification code is {code}. This code expires in 10 minutes.",
                $@"
                    <div style=""font-family:Arial,sans-serif;color:#111827;"">
                        <h2>Your Prestexa verification code</h2>
                        <p>Use the code below to complete your borrower login.</p>
                        <div style=""font-size:28px;font-weight:bold;letter-spacing:4px;margin:20px 0;"">
                            {code}
                        </div>
                        <p>This code expires in 10 minutes.</p>
                        <p>If you did not request this code, you can ignore this email.</p>
                    </div>
                "
            );

            var response = new Dictionary<string, object?>
            {
                ["requiresOtp"] = true,
                ["otpToken"] = otpToken,
                ["email"] = user.Email,
                ["message"] = "Verification code is required."
            };

            if (ShouldReturnOtpForTesting())
            {
                response["verificationCodeForTesting"] = code;
            }

            return Ok(response);
        }

        // ============================================================
        // VERIFY BORROWER OTP
        // ============================================================
        [HttpPost("verify-login")]
        public async Task<IActionResult> VerifyLogin([FromBody] PortalVerifyOtpRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = ValidateOtpTemporaryToken(
                request.OtpToken,
                "borrower_otp",
                request.Code
            );

            if (principal == null)
                return Unauthorized("Invalid or expired verification code.");

            var userIdValue = principal.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized("Invalid verification token.");

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized("User not found.");

            if (!IsBorrower(user))
                return Unauthorized("This account is not a borrower account.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            if (user.Company != null && !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            user.LastLoginAt = DateTime.UtcNow;

            await _trustedDeviceService.RememberAsync(
                user.Id,
                BorrowerPortal,
                request.DeviceId,
                BorrowerRememberDuration
            );

            await _context.SaveChangesAsync();

            var finalToken = await _authTokenService.CreateFinalJwtTokenAsync(
                user,
                BorrowerPortal
            );

            return Ok(new
            {
                token = finalToken,
                user = BuildUserResponse(user),
                message = "Borrower login successful."
            });
        }

        // ============================================================
        // FORGOT PASSWORD
        // ============================================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] PortalForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null ||
                !IsBorrower(user) ||
                user.Status != UserStatus.Active ||
                (user.Company != null && !user.Company.IsActive))
            {
                return Ok(new
                {
                    message = "If an account exists with that email, password reset instructions have been sent."
                });
            }

            try
            {
                var resetToken = CreatePasswordResetToken(user, "borrower_password_reset");

                var resetBaseUrl =
                    _config["BORROWER_PASSWORD_RESET_URL"] ??
                    _config["Auth:BorrowerPasswordResetUrl"] ??
                    "https://auth.prestexa.com/account/reset-password";

                var separator = resetBaseUrl.Contains('?') ? "&" : "?";
                var resetUrl = $"{resetBaseUrl}{separator}token={Uri.EscapeDataString(resetToken)}";

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Reset your Prestexa borrower password",
                    $"We received a request to reset your borrower password. Use this link within {PasswordResetTokenMinutes} minutes: {resetUrl}",
                    $@"
                        <div style=""font-family:Arial,sans-serif;color:#111827;line-height:1.5;"">
                            <h2>Reset your borrower password</h2>
                            <p>We received a request to reset your borrower password.</p>
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
                _logger.LogError(ex, "Failed to send borrower password reset email for {Email}", normalizedEmail);

                return StatusCode(500, new
                {
                    message = "Unable to send reset instructions."
                });
            }

            return Ok(new
            {
                message = "If an account exists with that email, password reset instructions have been sent."
            });
        }

        // ============================================================
        // RESET PASSWORD
        // ============================================================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PortalResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var principal = ValidatePasswordResetToken(
                request.Token,
                "borrower_password_reset"
            );

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

            if (!IsBorrower(user))
                return Unauthorized("This account is not a borrower account.");

            if (user.Status != UserStatus.Active)
                return Unauthorized("User is not active.");

            if (user.Company != null && !user.Company.IsActive)
                return Unauthorized("Company is inactive.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Password has been reset successfully."
            });
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private static bool IsBorrower(User user)
        {
            return string.Equals(user.Role, "Borrower", StringComparison.OrdinalIgnoreCase);
        }

        private string CreateOtpTemporaryToken(User user, string purpose, string code)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var tokenId = Guid.NewGuid().ToString("N");
            var codeHash = HashOtpCode(code, tokenId, jwtKey);

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("Email", user.Email),
                new Claim("Purpose", purpose),
                new Claim("TokenId", tokenId),
                new Claim("CodeHash", codeHash)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(OtpTokenMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string CreatePasswordResetToken(User user, string purpose)
        {
            var jwtKey = _config["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key missing.");

            var claims = new[]
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim("Email", user.Email),
                new Claim("Purpose", purpose)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(PasswordResetTokenMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private ClaimsPrincipal? ValidatePasswordResetToken(string token, string expectedPurpose)
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

        private ClaimsPrincipal? ValidateOtpTemporaryToken(
            string token,
            string expectedPurpose,
            string enteredCode)
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
                var tokenId = principal.FindFirst("TokenId")?.Value;
                var expectedHash = principal.FindFirst("CodeHash")?.Value;

                if (!string.Equals(purpose, expectedPurpose, StringComparison.Ordinal))
                    return null;

                if (string.IsNullOrWhiteSpace(tokenId) || string.IsNullOrWhiteSpace(expectedHash))
                    return null;

                var cleanCode = new string(enteredCode.Where(char.IsDigit).ToArray());

                if (cleanCode.Length != 6)
                    return null;

                var actualHash = HashOtpCode(cleanCode, tokenId, jwtKey);

                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(actualHash),
                        Encoding.UTF8.GetBytes(expectedHash)))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private static string GenerateSixDigitCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");
        }

        private static string HashOtpCode(string code, string tokenId, string secret)
        {
            var payload = $"{code}:{tokenId}:{secret}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(bytes);
        }

        private bool ShouldReturnOtpForTesting()
        {
            return bool.TryParse(_config["Auth:ReturnOtpForTesting"], out var value) && value;
        }

        private static object BuildUserResponse(User user)
        {
            return new
            {
                user.Id,
                user.Email,
                user.Role,
                user.CompanyNmlsNumber,
                CompanyName = user.Company?.Name
            };
        }
    }
}