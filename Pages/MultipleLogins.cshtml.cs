using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PracticalAssignment.Pages
{
	public class MultipleLoginsModel : PageModel
	{
		private readonly ILogger<MultipleLoginsModel> _logger;

		public MultipleLoginsModel(ILogger<MultipleLoginsModel> logger)
		{
			_logger = logger; 
		}

		public void OnGet()
		{
			_logger.LogInformation("Navigated to MultipleLogins page.");
		}
	}
}