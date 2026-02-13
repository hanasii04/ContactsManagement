using ContactsManagement.Helpers;
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
		private const int PageSize = 10;
		public ContactsController(AppDbContext context)
		{
			_context = context;
		}
		[HttpGet]
		public async Task<IActionResult> Index(string searchString, int? userId, int pageNumber = 1)
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
				searchString = searchString.Trim();
				query = query.Where(c => c.FullName.Contains(searchString)
									  || c.PhoneNumber.Contains(searchString)
									  || c.Users.Email.Contains(searchString));
			}
			if (pageNumber < 1)
			{
				pageNumber = 1;
			}

			var contacts = query.OrderByDescending(c => c.CreatedAt);

			var paginatedContacts = await PaginatedList<Contacts>.CreateAsync(
				contacts,
				pageNumber,
				PageSize
			);

			ViewData["CurrentFilter"] = searchString;
			ViewData["CurrentUserId"] = userId; // Giữ lại userId để link chuyển trang k bị lỗi
			ViewData["CurrentPage"] = pageNumber;

			return View(paginatedContacts);
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
