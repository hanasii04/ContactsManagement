using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Auth
{
	public class RegisterDTO
	{
		[Required(ErrorMessage = "Họ tên không được bỏ trống")]
		[MaxLength(100)]
		[MinLength(5, ErrorMessage = "Họ tên quá ngắn, vui lòng nhập ít nhất 5 ký tự")]
		// Ý nghĩa: Cấm các số từ 0-9 và các ký tự đặc biệt phổ biến như ! @ # $ %...
		[RegularExpression(@"^[^0-9!@#$%^&*()_+=\[{\]};:<>|./?,]+$", ErrorMessage = "Họ tên không được chứa số hoặc ký tự đặc biệt")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Email không được bỏ trống")]
		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		[MaxLength(255)]
		public string Email { get; set; }

		[Required(ErrorMessage = "Mật khẩu không được bỏ trống")]
		[MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
		public string Password { get; set; }

		[Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
		[Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
		public string ConfirmPassword { get; set; }

	}
}
