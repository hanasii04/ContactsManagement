using ContactsManagement.DTOs.Profile;
using ContactsManagement.Models;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContactsManagement.Controllers
{
	[Authorize]
	public class ProfileController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		public ProfileController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_webHostEnvironment = webHostEnvironment;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var user = await _context.Users
				.Where(u => u.UserId == userId)
				.Select(u => new UserProfileDTO
				{
					UserId = u.UserId,
					Email = u.Email,
					FullName = u.FullName,
					AvatarPath = u.AvatarPath,
					Role = u.Role,
					CreatedAt = u.CreatedAt,
					UpdatedAt = u.UpdatedAt
				})
				.FirstOrDefaultAsync();

			// Trường hợp hy hữu: Cookie vẫn còn hạn nhưng user đã bị xóa trong DB
			// -> Đăng xuất luôn để xóa Cookie rác
			if (user == null)
			{
				return RedirectToAction("Logout", "Auth");
			}

			return View(user);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateProfile(UserProfileDTO model)
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			if (!ModelState.IsValid)
			{
				return View("Index", model);
			}

			var user = await _context.Users.FindAsync(userId);

			if (user == null)
			{
				return RedirectToAction("Logout", "Auth");
			}

			if (model.AvatarFile != null)
			{
				// Đường dẫn thư mục: wwwroot/images/avatars
				string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");

				// Tạo thư mục nếu chưa có
				if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

				// Tạo tên file unique
				string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
				string filePath = Path.Combine(uploadsFolder, uniqueFileName);

				// Lưu file
				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await model.AvatarFile.CopyToAsync(fileStream);
				}

				// Cập nhật đường dẫn vào DB
				user.AvatarPath = "/images/avatars/" + uniqueFileName;
			}

			// Kiểm tra email trùng
			var newEmail = model.Email?.Trim().ToLower();
			var currentEmail = user.Email?.Trim().ToLower();

			if (newEmail != currentEmail)
			{
				var emailExists = await _context.Users
					.AnyAsync(u => u.Email.ToLower() == newEmail && u.UserId != userId);

				if (emailExists)
				{
					ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác");
					// Gán lại ảnh cũ để không bị mất khi reload
					model.AvatarPath = user.AvatarPath;
					return View("Index", model);
				}

				user.Email = model.Email.Trim(); // Lưu email mới
			}

			user.FullName = model.FullName;
			user.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			await RefreshSignIn(user);

			TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
			return RedirectToAction("Index");
		}

		private async Task RefreshSignIn(ContactsManagement.Models.Users user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
				new Claim(ClaimTypes.Name, user.FullName),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.ToString()),
				new Claim("AvatarPath", user.AvatarPath ?? "")
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties { IsPersistent = true };

			// Đăng xuất user hiện tại và đăng nhập lại với thông tin mới
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(claimsIdentity),
				authProperties);
		}

		[HttpGet]
		public IActionResult ChangePassword()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!int.TryParse(userIdString, out int userId))
			{
				return RedirectToAction("Login", "Auth");
			}

			var user = await _context.Users.FindAsync(userId);
			// Trường hợp hy hữu: Cookie vẫn còn hạn nhưng user đã bị xóa trong DB
			// -> Đăng xuất luôn để xóa Cookie rác
			if (user == null) return RedirectToAction("Logout", "Auth");

			// Kiểm tra mật khẩu nhập vào có đúng không
			bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash);

			if (!isOldPasswordCorrect)
			{
				ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không chính xác");
				return View(model);
			}

			// Không cho phép mật khẩu mới trùng với mật khẩu cũ
			if (BCrypt.Net.BCrypt.Verify(model.NewPassword, user.PasswordHash))
			{
				ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với mật khẩu cũ");
				return View(model);
			}

			// Hash mật khẩu mới trước khi lưu
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
			user.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";

			ModelState.Clear();

			return View();
		}
	}
}
