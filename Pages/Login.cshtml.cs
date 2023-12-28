using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PracticalAssignment.Model;
using Ganss.XSS;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PracticalAssignment.ViewModels;

namespace PracticalAssignment.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; }

        public int MaximumPasswordAgeInMinutes { get; } = 180;

        private readonly SignInManager<ApplicationUserStuff> _signInManager;
        private readonly AuthDbContext _context;
        private readonly ILogger<LoginModel> _logger;
        private HtmlSanitizer _sanitizer;
        private readonly IEmailSender _emailSender;

        public LoginModel(SignInManager<ApplicationUserStuff> signInManager, AuthDbContext context, ILogger<LoginModel> logger, HtmlSanitizer sanitizer, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
            _sanitizer = sanitizer;
            _emailSender = emailSender;
        }

        public void OnGet()
        {
            _logger.LogInformation("Visited the login page.");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userContent = Request.Form["userHtmlContent"];
            var sanitizedContent = _sanitizer.Sanitize(userContent);

            _logger.LogInformation("OnPostAsync started");
            if (!ModelState.IsValid) return Page();

            var user = await _signInManager.UserManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                await LogFailureAndReturnPageAsync(LModel.Email, "LoginFailed", "User not found");
                return Page();
            }

            if (await _signInManager.UserManager.IsLockedOutAsync(user))
            {
                await LogFailureAndReturnPageAsync(user.Id, "Lockout");
                return Page();
            }

            var identityResult = await _signInManager.PasswordSignInAsync(LModel.Email, LModel.Password, LModel.RememberMe, false);
            if (!identityResult.Succeeded)
            {
                await HandleFailedLoginAsync(user);
                return Page();
            }

            // Calculate time since the last password change
            var passwordHistory = _context.PasswordHistory
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.DateChanged)
                .FirstOrDefault(); // Get the most recent password change entry

            if (passwordHistory != null)
            {
                var timeSinceLastChange = (DateTime.UtcNow - passwordHistory.DateChanged).TotalMinutes;

                if (timeSinceLastChange > MaximumPasswordAgeInMinutes)
                {
                    // Sign out the user
                    await _signInManager.SignOutAsync();
                    // Clear the session
                    HttpContext.Session.Clear();

                    ModelState.AddModelError(string.Empty, "Your password has expired. Please change your password now.");
                    _logger.LogWarning("User with ID: {UserId}'s password has expired.");
                    return RedirectToPage("/PasswordExpired");
                }
            }

            // Check if the user's email is confirmed
         
            if (user.EmailConfirmed)
            {

                await CreateNewSessionAsync(user);

                // Generate OTP using a suitable method
                var otpCode = GenerateOTP(); // Implement a function to generate a random OTP

                // Send OTP via email
                var emailBody = $"Your OTP is: {otpCode}";
                await _emailSender.SendEmailAsync(user.Email, "OTP for Login", emailBody);




                // Store the OTP and UserID in Session for later verification

                HttpContext.Session.SetString("OTP", otpCode);

                await _signInManager.SignOutAsync();
                

                return RedirectToPage("/2FAOTP");
            }


            // Captcha
            var recaptchaResponse = Request.Form["recaptcha_response"];
            if (!await IsReCaptchaValid(recaptchaResponse))
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return Page();
            }

            var existingSessionResult = await HandleExistingSessionsAsync(user);
            if (existingSessionResult != null) return existingSessionResult;

            await AuditLogAsync(user.Id, "LoginSuccess");
            await CreateNewSessionAsync(user);

            return RedirectToPage("/Index");
        }

        private async Task LogFailureAndReturnPageAsync(string userId, string actionType, string failureReason = "")
        {
            await AuditLogAsync(userId, actionType, failureReason);
            ModelState.AddModelError("", "Email Address or Password incorrect");
        }

        private async Task HandleFailedLoginAsync(ApplicationUserStuff user)
        {
            await AuditLogAsync(user.Id, "LoginFailed", "Invalid credentials");

            await _signInManager.UserManager.AccessFailedAsync(user);
            if (await _signInManager.UserManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError("", "Account is locked. Please try again later.");
            }
            else
            {
                ModelState.AddModelError("", "Email Address or Password incorrect");
            }
        }

        private async Task<IActionResult> HandleExistingSessionsAsync(ApplicationUserStuff user)
        {
            var sessionId = HttpContext.Session.Id;
            var existingActiveSession = await _context.ActiveSessions
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            _logger.LogInformation($"Current session ID: {sessionId}");
            _logger.LogInformation($"Existing sessions: {existingActiveSession}");

            if (existingActiveSession != null && existingActiveSession.SessionId != sessionId)
            {
                _logger.LogInformation($"Active session detected. Session ID: {existingActiveSession.SessionId}");
                await AuditLogAsync(user.Id, "MultipleSessionsError", "Active session detected.");

                HttpContext.Session.Clear();
                // Optionally, set existingActiveSession.IsActive to false to invalidate it

                // Sign out the user
                await _signInManager.SignOutAsync();

                TempData["LoginErrorMessage"] = "Another active session has been detected.";
                return RedirectToPage("/MultipleLogins");
            }

            _logger.LogInformation("No active sessions found.");
            return null;
        }

        private async Task AuditLogAsync(string userId, string actionType, string failureReason = "")
        {
            var auditLogEntry = new AuditLog
            {
                UserId = userId,
                ActionType = actionType,
                Timestamp = DateTime.UtcNow,
                FailureReason = failureReason ?? ""
            };

            _context.AuditLogs.Add(auditLogEntry);
            await _context.SaveChangesAsync();
        }

        private async Task CreateNewSessionAsync(ApplicationUserStuff user)
        {
            try
            {
                // Invalidate any existing active sessions for the user
                var existingSessions = _context.ActiveSessions.Where(s => s.UserId == user.Id);
                foreach (var session in existingSessions)
                {
                    session.IsActive = false; // Or _context.ActiveSessions.Remove(session);
                }

                await _context.SaveChangesAsync();

                // Create a new active session
                var sessionId = HttpContext.Session.Id;
                var activeSession = new ActiveSession
                {
                    UserId = user.Id,
                    SessionId = sessionId,
                    DeviceIdentifier = $"{Request.Headers["User-Agent"]}-{Request.HttpContext.Connection.RemoteIpAddress}",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(50), // Testing
                    IsActive = true
                };

                _context.ActiveSessions.Add(activeSession);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("UserId", user.Id);

                _logger.LogInformation("New session created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while creating a session: {ex.Message}");
            }
        }

        public async Task<bool> IsReCaptchaValid(string recaptchaResponse)
        {
            var client = new HttpClient();
            var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret=6LdECzspAAAAAAVCHOQQryYEmtBMBACo2q826mQT&response={recaptchaResponse}", new StringContent(""));
            var jsonString = await response.Content.ReadAsStringAsync();
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonString);
            return recaptchaResult.Success && recaptchaResult.Score > 0.5m; // adjust the score requirement as needed
        }

        private string GenerateOTP()
        {
            const string chars = "0123456789";
            var random = new Random();
            var otp = new string(Enumerable.Repeat(chars, 6)
                .Select(s => chars[random.Next(chars.Length)]).ToArray());
            return otp;
        }
    }
}
