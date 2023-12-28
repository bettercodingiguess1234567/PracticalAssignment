using Microsoft.AspNetCore.Identity;

namespace PracticalAssignment.Model
{
    public class ApplicationUserStuff : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? MobileNo { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public string? CreditCardNo { get; set; }

    }
}
