using ContactsManagement.DTOs.Auth;
using ContactsManagement.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ContactsManagement.Interfaces;

namespace ContactsManagement.Controllers
{
	public class AuthController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IEmailService _emailService;
		public AuthController(AppDbContext context, IEmailService emailService)
		{
			_context = context;
			_emailService = emailService;
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
                new Claim(ClaimTypes.Role, user.Role.ToString()), // Lưu Quyền (Admin/User)
				new Claim("AvatarPath", user.AvatarPath ?? "") 
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

		[HttpGet]
		public IActionResult ForgotPassword()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

				// Luôn hiển thị thông báo thành công
				// Không tiết lộ email có tồn tại hay không → Bảo mật
				if (user == null)
				{
					TempData["SuccessMessage"] = "Đã gửi link đặt lại mật khẩu, vui lòng kiểm tra email";
					return RedirectToAction("ForgotPasswordConfirmation");
				}

				var resetToken = Guid.NewGuid().ToString();
				user.ResetToken = resetToken;
				user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Hết hạn sau 15 phút
				user.UpdatedAt = DateTime.UtcNow;

				await _context.SaveChangesAsync();

				// Tạo đường link Reset Password
				var resetLink = Url.Action(
					"ResetPassword",
					"Auth",
					new { token = resetToken, email = user.Email },
					protocol: Request.Scheme); 

				await _emailService.SendPasswordResetEmailAsync(
					user.Email,
					user.FullName,
					resetLink);

				TempData["SuccessMessage"] = "Đã gửi link đặt lại mật khẩu, vui lòng kiểm tra email";
				return RedirectToAction("ForgotPasswordConfirmation");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.");
				return View(model);
			}
		}

		[HttpGet]
		public IActionResult ForgotPasswordConfirmation()
		{
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> ResetPassword(string token, string email)
		{
			if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
			{
				TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ";
				return RedirectToAction("Login");
			}

			// Tìm user có email và token giống với dữ liễu được gửi lên
			var user = await _context.Users
				.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);

			if (user == null)
			{
				TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ";
				return RedirectToAction("Login");
			}

			if (user.ResetTokenExpiry < DateTime.UtcNow)
			{
				TempData["ErrorMessage"] = "Link đặt lại mật khẩu đã hết hạn";
				return RedirectToAction("ForgotPassword");
			}

			var model = new ResetPasswordDTO
			{
				Token = token,
				Email = email
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
		{
			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var user = await _context.Users
					.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

				if (user == null || user.ResetTokenExpiry < DateTime.UtcNow)
				{
					ModelState.AddModelError("", "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
					return View(model);
				}

				user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
				user.ResetToken = null; // Xóa token
				user.ResetTokenExpiry = null;
				user.UpdatedAt = DateTime.UtcNow;

				await _context.SaveChangesAsync();

				TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
				return RedirectToAction("Login");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
				return View(model);
			}
		}
	}
}
