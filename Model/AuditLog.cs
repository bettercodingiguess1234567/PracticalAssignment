namespace PracticalAssignment.Model
{
	public class AuditLog
	{
		public int Id { get; set; }
		public string UserId { get; set; } // User's identifier, e.g., email or username
		public string ActionType { get; set; }
		public DateTime Timestamp { get; set; }
		public string FailureReason { get; set; } // Reason for failure, if applicable (e.g., "Invalid Password", "User Not Found")

	}
}
