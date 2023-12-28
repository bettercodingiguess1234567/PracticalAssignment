using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PracticalAssignment.Model;
using PracticalAssignment.Services;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;

namespace PracticalAssignment.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string MobileNo { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }

        public string SessionId { get; private set; }



        private readonly UserManager<ApplicationUserStuff> _userManager;

        public string DecryptedCreditCardNumber { get; private set; }

        public IndexModel(UserManager<ApplicationUserStuff> userManager)
        {
            _userManager = userManager;
            
        }

        public async Task<IActionResult> OnGetAsync()
        {

			// Retrieve session data
			var userName = HttpContext.Session.GetString("UserName");
			var userId = HttpContext.Session.GetString("UserId");


            // Check if the session variables are set, if not, redirect to Login page
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId))
            {
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme); // Sign out the user
                HttpContext.Session.Clear(); // Clear the session
                //Uh redirect to the Login page if the session has expired
                return RedirectToPage("/Login");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            MobileNo = user.MobileNo;
            BillingAddress = user.BillingAddress;
            ShippingAddress = user.ShippingAddress;


            // encryption service here
            var encryptionService = new EncryptionService("your-encryption-key-here");

            // Decrypt credit card number
            DecryptedCreditCardNumber = encryptionService.Decrypt(user.CreditCardNo);

            // Retrieve the session ID
            SessionId = HttpContext.Session.Id;

            return Page();
        }
    }
}
