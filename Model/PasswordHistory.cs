using System.ComponentModel.DataAnnotations;

namespace PracticalAssignment.Model
{
    public class PasswordHistory
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required]
        public DateTime DateChanged { get; set; }

    }
}
