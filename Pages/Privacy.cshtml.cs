using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PracticalAssignment.Services;
using PracticalAssignment.Model;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Web;

namespace PracticalAssignment.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly UserManager<ApplicationUserStuff> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PrivacyModel> _logger;

        [BindProperty]
        public string Email { get; set; }

        public bool Enable2FA { get; set; }

        public PrivacyModel(UserManager<ApplicationUserStuff> userManager, IEmailSender emailSender, ILogger<PrivacyModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Get the currently logged-in user
                var user = await _userManager.GetUserAsync(User);

                // Check if the user is authenticated
                if (user == null)
                {
                    _logger.LogInformation("User is not authenticated.");
                    return RedirectToPage("./Login"); // Redirect to the login page or handle as needed
                }

                _logger.LogInformation("Starting email verification process for user: {UserName}", user.UserName);

                // Check if the user has already confirmed their email
                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Email for user {UserName} is already confirmed.", user.UserName);
                    TempData["2FA enabled"] = "Your email is already confirmed and 2FA is enabled.";
                    return Page();
                }

                string callbackUrl;
                try
                {
                    // Generate the email verification token
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    // Encode the user ID and code for the callback URL
                    var userId = HttpUtility.UrlEncode(user.Id);
                    var encodedCode = HttpUtility.UrlEncode(code);

                    // Create callback URL to confirm email
                    callbackUrl = Url.Page(
                        "/Login",
                        pageHandler: null,
                        values: new { area = "Identity", userId, code = encodedCode },
                        protocol: Request.Scheme);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating the callback URL for email verification.");
                    throw;
                }

                // Send email
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Email Verification",
                    $"Please verify your email by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                _logger.LogInformation("Email verification email sent successfully to {Email}", user.Email);

                // Set EmailConfirmed to true
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user); // Update the user in the database

                return RedirectToPage("./EmailVerificationSent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification.");
                throw;
            }
        }

        public void OnGet()
        {
        }
    }
}
