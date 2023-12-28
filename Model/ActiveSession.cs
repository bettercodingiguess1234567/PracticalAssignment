


namespace PracticalAssignment.Model
{
	public class ActiveSession

	{
		public int Id { get; set; }
		public string UserId { get; set; }
		public string SessionId { get; set; }
		public string DeviceIdentifier { get; set; }
		public DateTime CreatedAt { get; set; }


		public bool IsActive { get; set; }

		public DateTime ExpiresAt { get; set; }

	}
}
