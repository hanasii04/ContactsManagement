using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.Models
{
	public class Users
	{
		[Key]
		public int UserId { get; set; }
		[Required]
		[EmailAddress]
		[MaxLength(100)]
		public string Email { get; set; }
		[Required]
		[MaxLength(100)]
		public string FullName { get; set; }
		public string? PasswordHash { get; set; }
		public UserRole Role { get; set; } = UserRole.User;
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
		[MaxLength(50)]
		public string ProviderName { get; set; } = "local"; // Mặc định là "local"
		[MaxLength(255)]
		public string? ProviderID { get; set; } // ID từ Google/Facebook
		public virtual ICollection<Contacts> Contacts { get; set; } = new List<Contacts>();
		public virtual ICollection<Categories> Categories { get; set; } = new List<Categories>();
	}
	public enum UserRole
	{
		User = 0,
		Admin = 1,
	}
}
