using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;

using PracticalAssignment.Model;

using Microsoft.Extensions.Logging;
using Ganss.XSS;
using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;

namespace PracticalAssignment.Pages
{
    public class ResetPasswordModel : PageModel
    {

        public int MinimumPasswordAgeInMinutes { get; } = 2; 
        /*public int MaximumPasswordAgeInMinutes { get; } = 15;*/



        private readonly UserManager<ApplicationUserStuff> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;
        private readonly IHttpContextAccessor _contextAccessor; // cause need to retrieve the userId from the query string
        private readonly AuthDbContext _context;

        public ResetPasswordModel(UserManager<ApplicationUserStuff> userManager, ILogger<ResetPasswordModel> logger, IHttpContextAccessor contextAccessor, AuthDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [MinLength(12, ErrorMessage = "Enter at least a 12 characters password")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{8,}$", ErrorMessage = "Passwords must be at least 12 characters long and contain at least an uppercase letter, lower case letter, digit and a symbol")]
            public string? Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }

            public string? Code { get; set; }
            public string? UserId { get; set; } // Added UserId property
        }

        public async Task<IActionResult> OnGetAsync(string code = null)
        {
            _logger.LogInformation("Entering OnGetAsync with code: {Code}", code);

            if (code == null)
            {
                _logger.LogError("A code must be supplied for password reset.");
                return BadRequest("A code must be supplied for password reset.");
            }
            else
            {
                // Retrieve the UserId from the query string
                var userId = _contextAccessor.HttpContext.Request.Query["userId"].ToString();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("A userId must be supplied for password reset.");
                    return BadRequest("A userId must be supplied for password reset.");
                }

                Input = new InputModel
                {
                    Code = code,
                    UserId = userId // Set UserId property
                };

                // Log the code and user details for troubleshooting
                _logger.LogInformation("Code from URL: {Code}", Input.Code);
                var user = await _userManager.FindByIdAsync(Input.UserId);
                if (user != null)
                {
                    _logger.LogInformation("User found with ID: {UserId}", Input.UserId);
                }
                else
                {
                    _logger.LogInformation("User not found with ID: {UserId}", Input.UserId);
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("OnPostAsync method is being executed.");

            if (!ModelState.IsValid)
            {

                _logger.LogInformation("ModelState is not valid.");
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogError($"ModelState Error: {error.ErrorMessage}");
                    }
                }

                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                _logger.LogError("User with the specified ID does not exist.");
                // Don't reveal that the user does not exist
                return RedirectToPage("/Error");
            }

            // Verify that the user's code matches the code in the URL
            var isCodeValid = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, UserManager<ApplicationUserStuff>.ResetPasswordTokenPurpose, Input.Code);

            if (!isCodeValid)
            {
                _logger.LogError("Invalid reset password token for user with ID: {UserId}", user.Id);
                return RedirectToPage("/Error");
            }

            // Check if the new password matches any of the hashed passwords in the user's password history
            var hashedNewPassword = _userManager.PasswordHasher.HashPassword(user, Input.Password);

            // Retrieve the user's password history (e.g., last two hashed passwords)
            var passwordHistory = _context.PasswordHistory
                .Where(ph => ph.UserId == user.Id)
                .OrderByDescending(ph => ph.DateChanged)
                .Take(2) // Adjust this to the number of passwords you want to check against
                .ToList();


            _logger.LogInformation("Retrieved password history for user with ID: {UserId}", user.Id);

            // Check if the new password matches any of the passwords in the history
            if (passwordHistory.Any(ph => _userManager.PasswordHasher.VerifyHashedPassword(user, ph.PasswordHash, Input.Password) != PasswordVerificationResult.Failed))
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse a password that you have used in your last two password changes.");
                _logger.LogWarning("User with ID: {UserId} attempted to reuse a previous password.", user.Id);

                return Page();
            }

            _logger.LogInformation("Password history check passed for user with ID: {UserId}", user.Id);



            // Check password age policy
            if (passwordHistory != null)
            {
                foreach (var historyItem in passwordHistory)
                {
                    var timeSinceLastChange = (DateTime.UtcNow - historyItem.DateChanged).TotalMinutes;

                    if (timeSinceLastChange < MinimumPasswordAgeInMinutes)
                    {
                        ModelState.AddModelError(string.Empty, $"You cannot change your password within {MinimumPasswordAgeInMinutes} minutes from the last change.");
                        _logger.LogWarning("User with ID: {UserId} attempted to change password too soon.");
                        return Page();
                    }

                    /*if (timeSinceLastChange > MaximumPasswordAgeInMinutes)
                    {
                        ModelState.AddModelError(string.Empty, "Your password has expired. Please change your password now.");
                        _logger.LogWarning("User with ID: {UserId}'s password has expired.");
                        return Page();
                    }*/
                }
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            _logger.LogInformation($"Reset password for user with ID: {user.Id}");

            if (result.Succeeded)
            {


                // Save the new hashed password to the user's password history
                _context.PasswordHistory.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PasswordHash = hashedNewPassword,
                    DateChanged = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                _logger.LogInformation("User's password has been reset successfully.");
                return RedirectToPage("/Login");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);

                    _logger.LogError($"Error resetting password for user with ID: {user.Id}. Error: {error.Description}");
                }
            }

            return Page();
        }
    }
}
