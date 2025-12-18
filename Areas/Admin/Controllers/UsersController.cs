using ContactsManagement.Areas.Admin.Models;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContactsManagement.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UsersController : Controller
	{
		private readonly AppDbContext _context;
		public UsersController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string searchString)
		{
			var query = _context.Users
				.AsNoTracking() // Tối ưu khi chỉ đọc dữ liệu, không cần theo dõi thay đổi
				.Where(u => u.Role == UserRole.User && u.IsActive == true) 
				.AsQueryable(); 

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();
				query = query.Where(u => u.FullName.Contains(searchString) || u.Email.Contains(searchString));
			}	

			var listUsers = await query.OrderByDescending(u => u.CreatedAt)
				.Select(u => new UserModel
				{
					UserId = u.UserId,
					FullName = u.FullName,
					Email = u.Email,
					CreatedAt = u.CreatedAt,
					IsActive = u.IsActive,
					TotalContacts = u.Contacts.Count
				})
				.ToListAsync();

			ViewData["CurrentFilter"] = searchString;
			return View(listUsers);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var user = await _context.Users
				.AsNoTracking()
				.Where(u => u.UserId == id && u.Role == UserRole.User)
				.Select(u => new UserModel
				{
					UserId = u.UserId,
					FullName = u.FullName,
					Email = u.Email,
					CreatedAt = u.CreatedAt,
					IsActive = u.IsActive,
					TotalContacts = u.Contacts.Count
				})
				.FirstOrDefaultAsync();

			if (user == null)
			{
				return NotFound();
			}

			return View(user);
		}
	}
}
