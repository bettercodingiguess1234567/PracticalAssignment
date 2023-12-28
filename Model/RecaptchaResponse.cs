using Newtonsoft.Json;

namespace PracticalAssignment.Model
{
	public class RecaptchaResponse
	{
		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("score")]
		public decimal Score { get; set; }
	}
}
