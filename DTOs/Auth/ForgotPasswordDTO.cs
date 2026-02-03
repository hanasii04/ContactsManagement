using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Auth
{
	public class ForgotPasswordDTO
	{
		[Required(ErrorMessage = "Vui lòng nhập email")]
		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		public string Email { get; set; }
	}
}
