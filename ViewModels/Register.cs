using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PracticalAssignment.ViewModels
{

    public class Register
    {
        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }


        [Required]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit card number must be 16 digits long.")]
        public string? CreditCardNo { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Please enter a valid 8-digit phone number.")]
        public string? MobileNo { get; set; }

        [Required]
        public string? BillingAddress { get; set; }

        [Required]
        public string? ShippingAddress { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
		[MinLength(12, ErrorMessage = "Enter at least a 12 characters password")]
		[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{8,}$", ErrorMessage = "Passwords must be at least 12 characters long and contain at least an uppercase letter, lower case letter, digit and a symbol")]
		public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        public string? ConfirmPassword { get; set; }

        // For the photo, we would typically handle the file upload separately from the form model
        // For example, you might use IFormFile in the actual registration action method to handle file uploads

        [Display(Name = "Enable 2FA")]
        public bool Enable2FA { get; set; }

    }
}
