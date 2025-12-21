using ContactsManagement.DTOs.Dashboard;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContactsManagement.Controllers
{
	[Authorize]
	public class DashboardController : Controller
	{
		private readonly AppDbContext _context;
		public DashboardController(AppDbContext context)
		{
			_context = context;
		}
		public async Task<IActionResult> Index()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (string.IsNullOrEmpty(userIdString))
			{
				return RedirectToAction("Login", "Auth");
			}

			// GetUserId trả về string nên phải convert sang int để so sánh
			var userId = int.Parse(userIdString);

			// Lấy ngày đầu tháng hiện tại
			var now = DateTime.Now;
			var startOfMonth = new DateTime(now.Year, now.Month, 1);

			var myContacts = _context.Contacts.Where(c => c.UserId == userId);

			var model = new DashboardDTO
			{
				TotalContact = await myContacts.CountAsync(),

				// Đếm số lượng tạo từ đầu tháng đến giờ
				NewContactsThisMonth = await myContacts
										.Where(c => c.CreatedAt >= startOfMonth)
										.CountAsync()
			};
			return View(model);
		}
	}
}
