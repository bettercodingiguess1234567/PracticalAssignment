using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PracticalAssignment.Model;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PracticalAssignment.Pages
{
    public class _2FAOTPModel : PageModel
    {
        [BindProperty]
        public string OTP { get; set; }

        private readonly SignInManager<ApplicationUserStuff> _signInManager;
        private readonly ILogger<_2FAOTPModel> _logger;

        public _2FAOTPModel(SignInManager<ApplicationUserStuff> signInManager, ILogger<_2FAOTPModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Verify OTP
            _logger.LogInformation("Verifying OTP...");

            // Retrieve OTP and UserId
            var storedOTP = HttpContext.Session.GetString("OTP");
            var userId = HttpContext.Session.GetString("UserId");

            // Check if OTP or UserId is missing
            if (string.IsNullOrEmpty(storedOTP) || string.IsNullOrEmpty(userId))
            {
                // Handle missing data
                _logger.LogError("OTP or UserId not found in the session. Please start the login process again.");
                ModelState.AddModelError(string.Empty, "OTP or UserId not found. Please start the login process again.");
                return Page();
            }

            // Sign in the user and remove OTP
            var user = await _signInManager.UserManager.FindByIdAsync(userId);

            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                HttpContext.Session.Remove("OTP");
                _logger.LogInformation($"User {user.UserName} signed in successfully.");
                return RedirectToPage("/Index");
            }
            else
            {
                // Handle user not found
                _logger.LogError($"User with ID {userId} not found.");
                ModelState.AddModelError(string.Empty, "User not found. Please start the login process again.");
                return Page();
            }
        }
    }
}
