namespace ContactsManagement.Areas.Admin.Models
{
	public class DashboardModel
	{
		public int TotalUsers { get; set; }          // Tổng số người dùng
		public int TotalContacts { get; set; }       // Tổng số liên hệ trong hệ thống
		public int NewUsersToday { get; set; }       // Số người đăng ký hôm nay
		public int ActiveUsers { get; set; }         // Số tài khoản đang hoạt động
		public int InactiveUsers { get; set; }       // Số tài khoản bị khóa
	}
}
