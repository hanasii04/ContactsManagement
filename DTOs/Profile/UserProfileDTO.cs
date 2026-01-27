using ContactsManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Profile
{
	public class UserProfileDTO
	{
		public int UserId { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập email")]
		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		[StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập họ tên")]
		[MaxLength(100)]
		// Ý nghĩa: Cấm các số từ 0-9 và các ký tự đặc biệt phổ biến như ! @ # $ %...
		[RegularExpression(@"^[^0-9!@#$%^&*()_+=\[{\]};:<>|./?,]+$", ErrorMessage = "Họ tên không được chứa số hoặc ký tự đặc biệt")]
		[MinLength(5, ErrorMessage = "Họ tên quá ngắn, vui lòng nhập ít nhất 5 ký tự")]
		public string FullName { get; set; }

		public string? AvatarPath { get; set; }

		public UserRole Role { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public IFormFile? AvatarFile { get; set; }
	}
}