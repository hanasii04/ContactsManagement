using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.Models
{
	public class Contacts
	{
		[Key]
		public int ContactId { get; set; }
		[MaxLength(10)]
		public string PhoneNumber { get; set; }
		[Required]
		[MaxLength(100)]
		public string FullName { get; set; }
		[EmailAddress]
		[MaxLength(100)]
		public string? Email { get; set; }
		[MaxLength(255)]
		public string? Address { get; set; }
		public string? Notes { get; set; }
		public bool IsDeleted { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
		[ForeignKey("User")]
		public int UserId { get; set; }
		public virtual Users Users { get; set; }
		public virtual ICollection<ContactCategories> ContactCategories { get; set; } = new List<ContactCategories>();
	}
}
