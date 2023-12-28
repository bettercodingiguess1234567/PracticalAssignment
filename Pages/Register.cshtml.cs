using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PracticalAssignment.Model;
using PracticalAssignment.ViewModels;
using PracticalAssignment.Services;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Ganss.XSS;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;



namespace PracticalAssignment.Pages
{
    public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUserStuff> UserManager { get; }
        private SignInManager<ApplicationUserStuff> SignInManager { get; }
		private readonly AuthDbContext _context;
        private HtmlSanitizer _sanitizer;
        private readonly ILogger<LoginModel> _logger;



        [BindProperty]
        public Register RModel { get; set; }
		public RegisterModel(UserManager<ApplicationUserStuff> userManager,
        SignInManager<ApplicationUserStuff> signInManager, AuthDbContext context, HtmlSanitizer sanitizer, ILogger<LoginModel> logger)
        {
            this.UserManager = userManager;
            this.SignInManager = signInManager;
			_context = context;
            _sanitizer = sanitizer;
            _logger = logger;


        }
        //Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            var userContent = Request.Form["userHtmlContent"];
            var sanitizedContent = _sanitizer.Sanitize(userContent);

          

            if (ModelState.IsValid)
            {
                var encryptionService = new EncryptionService("your-encryption-key-here");
                var encryptedCreditCardNumber = encryptionService.Encrypt(RModel.CreditCardNo);

                var user = new ApplicationUserStuff()
                {
                    UserName = RModel.Email,
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Email = RModel.Email,
                    MobileNo = RModel.MobileNo,
                    BillingAddress = RModel.BillingAddress,
                    ShippingAddress = RModel.ShippingAddress,
                    CreditCardNo = encryptedCreditCardNumber
                };
                var result = await UserManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
					// Log the successful registration
					await AuditLogAsync(user.Id, "RegistrationSuccess");
                    _logger.LogInformation("RegistrationSuccess: User registered successfully.");

                    // Save the new hashed password to the user's password history
                    _context.PasswordHistory.Add(new PasswordHistory
                    {
                        UserId = user.Id,
                        PasswordHash = UserManager.PasswordHasher.HashPassword(user, RModel.Password),
                        DateChanged = DateTime.UtcNow
                    });

                    try
                    {
                        await _context.SaveChangesAsync(); // Commit changes to the database

                        // Log that password history was updated successfully
                        _logger.LogInformation("Password history updated successfully.");

                        await SignInManager.SignInAsync(user, false);
                        return RedirectToPage("/Index");
                    }
                    catch (Exception ex)
                    {
                        // Log the exception if there's an issue with saving to the database
                        _logger.LogError($"Error updating password history: {ex.Message}");
                        // Handle the exception or return an error page as needed
                    }
                }
                else
                {

					// Log the failed registration

			string failureReasons = string.Join(", ", result.Errors.Select(e => e.Description));
					await AuditLogAsync(null, "RegistrationFailed", failureReasons);
				}
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName")

                        ModelState.AddModelError("RModel.Email", "Email address is already taken.");
                    else
                    {
						// Log the failed registration attempt due to invalid model state
						await AuditLogAsync(null, "RegistrationFailed", "Invalid ModelState");
						ModelState.AddModelError("", error.Description);
                    }
                }

            }
            return Page();
        }

        
        private async Task AuditLogAsync(string userId, string actionType, string failureReason = "")
		{
			var auditLogEntry = new AuditLog
			{
				UserId = userId ?? "Anonymous", // If the user is not created, use "Anonymous" or some other placeholder
				ActionType = actionType,
				Timestamp = DateTime.UtcNow,
				FailureReason = failureReason
			};

			_context.AuditLogs.Add(auditLogEntry);
			await _context.SaveChangesAsync();
		}


	}
}

