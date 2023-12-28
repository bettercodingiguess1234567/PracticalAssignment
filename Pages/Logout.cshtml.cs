using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PracticalAssignment.Model;
using Microsoft.EntityFrameworkCore;
using PracticalAssignment.Model;
using System.Threading.Tasks;


namespace PracticalAssignment.Pages
{
    public class LogoutModel : PageModel
    {

		private readonly SignInManager<ApplicationUserStuff> signInManager;

        private readonly IHttpContextAccessor contxt;
		private readonly AuthDbContext _context;
		public LogoutModel(SignInManager<ApplicationUserStuff> signInManager, IHttpContextAccessor contxt, AuthDbContext context)
		{
			this.signInManager = signInManager;
            this.contxt = contxt;
			_context = context;
        }
		public void OnGet() { }

		public async Task<IActionResult> OnPostLogoutAsync()
		{
			var user = await signInManager.UserManager.GetUserAsync(User);
			if (user != null)
			{
				var userId = user.Id;


				// Retrieve all active sessions for this user
				var activeSessions = _context.ActiveSessions.Where(s => s.UserId == userId && s.IsActive); 

				// Mark all sessions as inactive or remove them
				foreach (var session in activeSessions)
				{
					// Option 1: Remove the session
					_context.ActiveSessions.Remove(session);

					
				}
				await _context.SaveChangesAsync();
			}

			// Clear the session and sign out
			HttpContext.Session.Clear();
			await signInManager.SignOutAsync();


			Response.Cookies.Delete(".AspNetCore.Cookies");

			return RedirectToPage("/Login");
		}
		public async Task<IActionResult> OnPostDontLogoutAsync()
		{
			return RedirectToPage("Index");
		}

	}
}
