using ContactsManagement.Areas.Admin.Models;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContactsManagement.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class DashboardController : Controller
	{
		private readonly AppDbContext _context;
		public DashboardController(AppDbContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> Index()
		{
			// Lấy ngày hôm nay (để tính New Users Today)
			var today = DateTime.UtcNow.Date;

			// Tạo View và điền dữ liệu
			var dashboardModel = new DashboardModel
			{
				// Đếm tổng user,  trừ admin
				TotalUsers = await _context.Users.CountAsync(u => u.Role == UserRole.User),

				// Đếm tổng contact
				TotalContacts = await _context.Contacts.CountAsync(),

				// Đếm user đăng ký từ 00:00 hôm nay
				NewUsersToday = await _context.Users
								.CountAsync(u => u.CreatedAt >= today && u.Role == UserRole.User),

				// Đếm tài khoản theo trạng thái
				ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role == UserRole.User),
				InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive && u.Role == UserRole.User),
			};
			return View(dashboardModel);
		}
	}
}
