using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Categories
{
	public class CategoriesDTO
	{
		public int CategoriesId { get; set; }
		public int UserId { get; set; }
		[Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
		public string Name { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
