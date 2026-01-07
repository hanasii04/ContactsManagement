using ContactsManagement.DTOs.Categories;
using ContactsManagement.DTOs.Contact;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContactsManagement.Controllers
{
	[Authorize]
	public class ContactsController : Controller
	{
		private readonly AppDbContext _context;
		public ContactsController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index(string searchString)
		{
			// Lấy UserId từ Claims rồi ép kiểu về int
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) 
				return RedirectToAction("Login", "Auth");

			var query = _context.Contacts
				.AsNoTracking()
				.Where(c => c.UserId == userId && c.IsDeleted == false)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				searchString = searchString.Trim();

				query = query.Where(c => c.FullName.Contains(searchString)
									  || c.PhoneNumber.Contains(searchString)
									  || c.Email.Contains(searchString));
			}	

			var myContacts = await query.OrderByDescending(c => c.CreatedAt)
				.Select(c => new ContactDTO
				{ 
					ContactId = c.ContactId,
					FullName = c.FullName,
					PhoneNumber = c.PhoneNumber,
					Email = c.Email,
					CreatedAt = c.CreatedAt
				})
				.ToListAsync();

			ViewData["CurrentFilter"] = searchString;
			return View(myContacts);
		}

		[HttpGet]
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) 
				return RedirectToAction("Login", "Auth");

			var contact = await _context.Contacts
				.AsNoTracking()
				.Include(c => c.ContactCategories)
				.ThenInclude(cc => cc.Categories) 
				.FirstOrDefaultAsync(m => m.ContactId == id && m.UserId == userId);

			if (contact == null) return NotFound();

			var categoryNames = contact.ContactCategories
										   .Select(cc => cc.Categories.Name)
										   .ToList();
			var model = new ContactDTO
			{
				ContactId = contact.ContactId,
				FullName = contact.FullName,
				PhoneNumber = contact.PhoneNumber,
				Email = contact.Email,
				Address = contact.Address,
				Notes = contact.Notes,
				CreatedAt = contact.CreatedAt,
				UpdatedAt = contact.UpdatedAt,
				CategoryNames = categoryNames
			};

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Create()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) 
				return RedirectToAction("Login", "Auth");

			await LoadCategoriesToViewBag(userId);
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ContactDTO model)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			if (!ModelState.IsValid)
			{
				await LoadCategoriesToViewBag(userId);
				return View(model);
			}

			var contact = new Contacts
			{
				FullName = model.FullName,
				PhoneNumber = model.PhoneNumber,
				Email = model.Email,
				Address = model.Address,
				Notes = model.Notes,
				CreatedAt = DateTime.UtcNow,
				UserId = userId,
			};

			// Kiểm tra xem list CategoryIds có null hoặc rỗng không
			if (model.CategoryIds != null && model.CategoryIds.Any())
			{
				// Lấy ra list CategoryId thuộc về user hiện tại và nằm trong list CategoryId gửi lên
				var validCategoryIds = await _context.Categories
					.Where(c => c.UserId == userId && model.CategoryIds.Contains(c.CategoriesId))
					.Select(c => c.CategoriesId)
					.ToListAsync();

				foreach (var categoryId in validCategoryIds)
				{
					// Tạo mới ContactCategories thông qua navigation property với Contacts
					contact.ContactCategories.Add(new ContactCategories
					{
						CategoriesId = categoryId
						// ContactId sẽ được EF tự động gán khi lưu contact thành công
					});
				}
			}

			_context.Contacts.Add(contact);
			await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}

		[HttpGet]
		public async Task<IActionResult> Update(int? id)
		{
			if (id == null) return NotFound();

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			// Lấy ra liên hệ cần sửa, phải đúng với id đã click và thuộc về user hiện tại
			var contact = await _context.Contacts
				.AsNoTracking()
				.Include(c => c.ContactCategories) // nối bảng ContactCategories để lấy thông tin danh mục
				.FirstOrDefaultAsync(c => c.ContactId == id && c.UserId == userId);

			if (contact == null) return NotFound();

			var categoryIds = contact.ContactCategories
									.Select(cc => cc.CategoriesId)
									.ToList();

			// Trả về DTO thay vì contact.FullName
			// để tận dụng các attribute validation trong DTO
			// sau này nếu có thêm trường khác thì chỉ cần sửa DTO
			var model = new ContactDTO
			{
				ContactId = contact.ContactId,
				FullName = contact.FullName,
				PhoneNumber = contact.PhoneNumber,
				Email = contact.Email,
				Address = contact.Address,
				Notes = contact.Notes,
				CreatedAt = contact.CreatedAt,
				UpdatedAt = contact.UpdatedAt,
				CategoryIds = categoryIds
			};

			await LoadCategoriesToViewBag(userId);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(int id, ContactDTO model)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			// Lấy ra liên hệ cần sửa, phải đúng với id đã click và thuộc về user hiện tại
			var contact = await _context.Contacts
				.Include(c => c.ContactCategories)
				.FirstOrDefaultAsync(c => c.ContactId == id && c.UserId == userId);

			if (contact == null) return NotFound();

			contact.FullName = model.FullName; 
			contact.PhoneNumber = model.PhoneNumber;
			contact.Email = model.Email;
			contact.Address = model.Address;
			contact.Notes = model.Notes;
			contact.UpdatedAt = DateTime.UtcNow;

			// Xóa tất cả các danh mục mà liên hệ này đang có
			_context.ContactCategories.RemoveRange(contact.ContactCategories);

			// Kiểm tra xem list CategoryIds user gửi lên có null hoặc rỗng không
			if (model.CategoryIds != null && model.CategoryIds.Any())
			{
				// Lấy ra các CategoryId thuộc về user hiện tại và nằm trong list CategoryId gửi lên
				var validCategoryIds = await _context.Categories
													 .Where(c => c.UserId == userId && model.CategoryIds.Contains(c.CategoriesId))
													 .Select(c => c.CategoriesId)
													 .ToListAsync();

				// Duyệt qua list các CategoryId hợp lệ và tạo các bản ghi trung gian
				foreach (var categoryId in validCategoryIds)
				{
					// Tạo mới ContactCategories thông qua navigation property với Contacts
					contact.ContactCategories.Add(new ContactCategories
					{
						ContactId = contact.ContactId,
						CategoriesId = categoryId
					});
				}
			}

			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId)) return RedirectToAction("Login", "Auth");

			var contact = await _context.Contacts
				.FirstOrDefaultAsync(c => c.ContactId == id && c.UserId == userId);

			if (contact == null) return NotFound();

			contact.IsDeleted = true;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Đã xóa liên hệ";
			return RedirectToAction("Index");
		}

		// Hàm lấy thông tin danh mục của user hiện tại
		private async Task LoadCategoriesToViewBag(int userId)
		{
			var categories = await _context.Categories
										   .Where(c => c.UserId == userId)
										   .OrderBy(c => c.Name)
										   .ToListAsync();

			ViewData["CategoryId"] = new SelectList(categories, "CategoriesId", "Name");
		}
	}
}
