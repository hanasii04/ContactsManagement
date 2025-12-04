using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Auth
{
	public class LoginDTO
	{
		[Required(ErrorMessage = "Vui lòng nhập Email")]
		[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
		public string Password { get; set; }
	}
}
