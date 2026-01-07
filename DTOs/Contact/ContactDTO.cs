using System.ComponentModel.DataAnnotations;

namespace ContactsManagement.DTOs.Contact
{
	public class ContactDTO
	{
		public int ContactId { get; set; }
		[Required(ErrorMessage = "Vui lòng nhập họ tên")]
		[StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
		[RegularExpression(@"^[^0-9!@#$%^&*()_+=\[{\]};:<>|./?,]+$", ErrorMessage = "Họ tên không được chứa số hoặc ký tự đặc biệt")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
		[RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
		[StringLength(10, ErrorMessage = "Số điện thoại không được quá 10 ký tự")]
		public string PhoneNumber { get; set; }

		[EmailAddress(ErrorMessage = "Email không hợp lệ")]
		[StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
		public string? Email { get; set; }

		[StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
		public string? Address { get; set; }
		public string? Notes { get; set; }

		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
		public DateTime CreatedAt { get; set; }

		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
		public DateTime? UpdatedAt { get; set; }
		public List<int> CategoryIds { get; set; } = new List<int>();
		public List<string> CategoryNames { get; set; } = new List<string>();
	}
}
