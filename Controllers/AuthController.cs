using ContactsManagement.DTOs.Auth;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ContactsManagement.Controllers
{
	public class AuthController : Controller
	{
		private readonly AppDbContext _context;
		public AuthController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Register(RegisterDTO registerDTO)
		{
			if (!ModelState.IsValid)
			{
				return View(registerDTO);
			}
			// Kiểm tra email đã tồn tại chưa
			var emailExits = await _context.Users.AnyAsync(u => u.Email == registerDTO.Email);
			if (emailExits)
			{
				ModelState.AddModelError("Email", "Email đã được sử dụng");
				return View(registerDTO);
			}

			// Băm mật khẩu
			var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDTO.Password);

			var newUser = new Users
			{
				FullName = registerDTO.FullName,
				Email = registerDTO.Email,
				PasswordHash = passwordHash,
				Role = UserRole.User,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				ProviderName = "local" // Đánh dấu đây là tk local, tk Google/Facebook sẽ có ProviderName khác
			};
			_context.Add(newUser);
			await _context.SaveChangesAsync();
			return RedirectToAction("Login", "Auth");
		}

		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> Login(LoginDTO loginDTO)
		{
			if (!ModelState.IsValid)
			{
				return View(loginDTO);
			}

			// Kiểm tra email và mật khẩu
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);
			if (user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.PasswordHash)) // so sánh mật khẩu
			{
				// login thất bại là do email hoặc mật khẩu không đúng
				// nên dùng string.Empty để thêm lỗi chung cho cả form
				// tránh báo lỗi riêng lẻ từng trường hợp
				ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng");
				return View(loginDTO);
			}

			if (!user.IsActive)
			{
				ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên");
				return View(loginDTO);
			}

			// Tạo các claim để lưu thông tin người dùng trong cookie
			// claims là các thông tin sẽ được lưu trong cookie
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // Lưu ID
                new Claim(ClaimTypes.Name, user.FullName), // Lưu Tên
                new Claim(ClaimTypes.Email, user.Email), // Lưu Email
                new Claim(ClaimTypes.Role, user.Role.ToString()) // Lưu Quyền (Admin/User)
            };

			// Tạo ClaimsIdentity
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

			// Cấu hình hạn sử dụng cookie
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true, // Giữ đăng nhập ngay cả khi đóng trình duyệt
				ExpiresUtc = DateTime.UtcNow.AddDays(7) // Hết hạn sau 7 ngày
			};

			// Lưu cookie
			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(claimsIdentity),
				authProperties);

			// Chuyển hướng sau khi đăng nhập thành công
			// Nếu là Admin -> Về trang Dashboard, User -> Về trang chủ
			if (user.Role == UserRole.Admin)
			{
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
			}

			return RedirectToAction("Index", "Dashboard");
		}

		[HttpGet]
		public async Task<IActionResult> Logout()
		{
			// Tìm Cookie đăng nhập và xóa
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			return RedirectToAction("Login", "Auth");
		}
	}
}
