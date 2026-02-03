using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Auth
{
	public class ResetPasswordDTO
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Token { get; set; } // Token này sẽ được bind từ URL xuống (hidden input)

		[Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
		[MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
		public string NewPassword { get; set; }

		[Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
		[Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
		public string ConfirmPassword { get; set; }
	}
}
