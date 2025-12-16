using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContactsManagement.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class ContactsController : Controller
	{
		private readonly AppDbContext _context;
		public ContactsController(AppDbContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> Index(string searchString, int? userId)
		{
			var query = _context.Contacts
				.Include(c => c.Users)
				.AsQueryable(); // Tạo truy vấn nhưng chưa thực thi ngay, cho phép thêm điều kiện lọc
								// tránh việc truy vấn toàn bộ data trước khi lọc => tối ưu hiệu suất

			// Lọc theo UserId nếu có
			if (userId != null)
			{
				query = query.Where(c => c.UserId == userId);
			}

			if (!string.IsNullOrEmpty(searchString))
			{
				query = query.Where(c => c.FullName.Contains(searchString)
									  || c.PhoneNumber.Contains(searchString)
									  || c.Users.Email.Contains(searchString));
			}

			var contacts = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

			// Giữ lại giá trị tìm kiếm để hiển thị trong View
			ViewData["CurrentFilter"] = searchString;

			return View(contacts);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			// Chặn trường hợp id null trong URL, tránh sập web
			// id ở đây là id trong URL: /Admin/Contacts/Details/5
			if (id == null)
			{
				return NotFound();
			}

			// Nối với 3 bảng để lấy thông tin chi tiết của liên hệ
			var contact = await _context.Contacts
				.Include(c => c.Users)
				.Include(c => c.ContactCategories)
				.ThenInclude(cc => cc.Categories)
				.FirstOrDefaultAsync(c => c.ContactId == id);

			// Kiểm tra id có tồn tại trong db hay không
			if (contact == null)
			{
				return NotFound();
			}

			return View(contact);
		}
	}
}
