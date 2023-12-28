using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PracticalAssignment.Services;
using PracticalAssignment.Model;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
namespace PracticalAssignment.Pages
{
    public class PasswordExpiredModel : PageModel
    {

        private readonly UserManager<ApplicationUserStuff> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PasswordExpiredModel> _logger;

        [BindProperty]
        public string Email { get; set; }

        public PasswordExpiredModel(UserManager<ApplicationUserStuff> userManager, IEmailSender emailSender, ILogger<PasswordExpiredModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;

        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Starting password reset process for email: {Email}", Email);

                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(Email))
                    {
                        ModelState.AddModelError(string.Empty, "Email is required.");
                        return Page();
                    }


                    var user = await _userManager.FindByEmailAsync(Email);
                    if (user == null) //|| !(await _userManager.IsEmailConfirmedAsync(user)))
                    {
                        // Don't reveal that the user does not exist or is not confirmed
                        _logger.LogInformation("User with email {Email} not found or not confirmed.", Email);
                        return RedirectToPage("./LinkSent");
                    }

                    _logger.LogInformation("User found and email is confirmed.");

                    // Generate the reset password token
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // Create callback URL to reset password
                    var callbackUrl = Url.Page(
                        "/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code },
                        protocol: Request.Scheme);

                    // Send email
                    await _emailSender.SendEmailAsync(
                        Email,
                        "Reset Password",
                        $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    _logger.LogInformation("Password reset email sent successfully to {Email}", Email);

                    return RedirectToPage("./LinkSent");
                }

                _logger.LogInformation("Model state is not valid.");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset for email: {Email}", Email);
                throw;
            }
        }


        public void OnGet()
        {
        }
    }
}
