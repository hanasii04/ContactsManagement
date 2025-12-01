using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.Models
{
	public class Categories
	{
		[Key]
		public int CategoriesId { get; set; }
		[Required]
		[MaxLength(50)]
		public string Name { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }
		[ForeignKey("User")]
		public int UserId { get; set; }
		public virtual Users Users { get; set; }
		public virtual ICollection<ContactCategories> ContactCategories { get; set; } = new List<ContactCategories>();
	}
}
