using ContactsManagement.DTOs.Categories;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContactsManagement.Controllers
{
	[Authorize]
	public class CategoriesController : Controller
	{
		private readonly AppDbContext _context;
		public CategoriesController(AppDbContext context)
		{ 
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string searchString)
		{
			// Lấy UserId từ Claims rồi ép kiểu về int
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			var query = _context.Categories
				.Where(c => c.UserId == userId)
				.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				query = query.Where(c => c.Name.Contains(searchString.Trim()));
			}

			var categories = await query.OrderByDescending(c => c.CreatedAt)
				.Select(c => new CategoriesDTO
				{
					CategoriesId = c.CategoriesId,
					Name = c.Name,
					CreatedAt = c.CreatedAt,
					UpdatedAt = c.UpdatedAt
				})
				.ToListAsync();

			ViewData["CurrentFilter"] = searchString;
			return View(categories);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			var category = await _context.Categories
				.AsNoTracking()
				.Where(c => c.UserId == userId && c.CategoriesId == id)
				.Select(c => new CategoriesDTO
				{
					CategoriesId = c.CategoriesId,
					Name = c.Name,
					CreatedAt = c.CreatedAt,
					UpdatedAt = c.UpdatedAt
				})
				.FirstOrDefaultAsync();

			if (category == null)
			{
				return NotFound();
			}	
			return View(category);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Create(CategoriesDTO model)
		{
			if (!ModelState.IsValid) 
			{
				return View(model);
			}

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			string cleanName = model.Name.Trim();

			// Kiểm tra user hiện tại đã có danh mục này chưa
			bool isDuplicate = await _context.Categories
				.AnyAsync(c => c.UserId == userId && c.Name.ToLower() == cleanName.ToLower());

			if (isDuplicate)
			{
				ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại");
				return View(model);
			}

			var category = new Categories
			{
				Name = cleanName,
				UserId = userId,
				CreatedAt = DateTime.UtcNow
			};

			_context.Categories.Add(category);
			await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}

		[HttpGet]
		public async Task<IActionResult> Update(int? id)
		{
			if (id == null) return NotFound();

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			// Lấy ra danh mục cần sửa, phải đúng với id đã click và thuộc về user hiện tại
			var category = await _context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(c => c.CategoriesId == id && c.UserId == userId);

			if (category == null) return NotFound();

			// Trả về DTO thay vì category.Name
			// để tận dụng các attribute validation trong DTO
			// sau này nếu có thêm trường khác thì chỉ cần sửa DTO
			var model = new CategoriesDTO
			{
				Name = category.Name
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(int id, CategoriesDTO model)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			string cleanName = model.Name.Trim();

			// Kiểm tra xem user hiện tại có danh mục nào khác với id đang sửa mà trùng tên không
			bool isDuplicate = await _context.Categories.AnyAsync(c =>
				c.UserId == userId &&
				c.CategoriesId != id && // Loại id đang sửa
				c.Name.ToLower() == cleanName.ToLower());

			if (isDuplicate)
			{
				ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại!");
				return View(model);
			}

			// Tìm lại danh mục cần sửa
			var category = await _context.Categories
				.FirstOrDefaultAsync(c => c.CategoriesId == id && c.UserId == userId);

			if (category == null) return NotFound();

			category.Name = cleanName;
			category.UpdatedAt = DateTime.Now;

			await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}

	}
}
