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

			await SignInUserAsync(user);

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

		[HttpPost] // Dùng HttpPost để bảo mật hơn HttpGet (chống CSRF)
		public IActionResult ExternalLogin(string provider, string returnUrl = null)
		{
			if (provider != "Google" && provider != "Facebook")
			{
				return BadRequest("Nhà cung cấp không hợp lệ.");
			}

			var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { ReturnUrl = returnUrl });
			var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

			return Challenge(properties, provider);
		}

		/// <summary>
		/// Nơi hứng dữ liệu trả về từ Google/Facebook
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
		{
			returnUrl ??= Url.Content("~/");

			// 1. Kiểm tra lỗi từ Facebook/Google (người dùng hủy đăng nhập...)
			if (remoteError != null)
			{
				TempData["ErrorMessage"] = $"Lỗi từ nhà cung cấp: {remoteError}";
				return RedirectToAction("Login");
			}

			// 2. Đọc vé tạm từ "Phòng chờ" (ExternalCookie) - BẢO MẬT CỐT LÕI
			var info = await HttpContext.AuthenticateAsync("ExternalCookie");
			if (!info.Succeeded || info.Principal == null)
			{
				TempData["ErrorMessage"] = "Không thể lấy thông tin xác thực. Vui lòng thử lại.";
				return RedirectToAction("Login");
			}

			// 3. Rút trích dữ liệu
			var claims = info.Principal.Claims.ToList();
			var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
			var nameIdentifier = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			var pictureUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value;
			var provider = info.Principal.Identity.AuthenticationType; // "Google" hoặc "Facebook"

			if (string.IsNullOrEmpty(email))
			{
				TempData["ErrorMessage"] = "Không lấy được email từ tài khoản mạng xã hội của bạn.";
				return RedirectToAction("Login");
			}

			// 4. Truy vấn Database
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
			bool isNewUser = false;

			if (user != null)
			{
				// TÀI KHOẢN ĐÃ TỒN TẠI -> Kiểm tra khóa
				if (!user.IsActive)
				{
					await HttpContext.SignOutAsync("ExternalCookie"); // Hủy vé tạm
					TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
					return RedirectToAction("Login");
				}

				// Cập nhật thông tin Provider và Avatar nếu có sự thay đổi
				bool needUpdate = false;
				if (user.ProviderName != provider || user.ProviderID != nameIdentifier)
				{
					user.ProviderName = provider;
					user.ProviderID = nameIdentifier;
					needUpdate = true;
				}

				if (!string.IsNullOrEmpty(pictureUrl) && user.AvatarPath != pictureUrl)
				{
					user.AvatarPath = pictureUrl;
					needUpdate = true;
				}

				if (needUpdate)
				{
					user.UpdatedAt = DateTime.UtcNow;
					await _context.SaveChangesAsync();
				}
			}
			else
			{
				// TÀI KHOẢN CHƯA TỒN TẠI -> Tạo mới
				isNewUser = true;
				user = new Users
				{
					Email = email,
					FullName = name ?? email.Split('@')[0],
					ProviderName = provider,
					ProviderID = nameIdentifier,
					AvatarPath = pictureUrl,
					Role = UserRole.User,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				_context.Users.Add(user);
				await _context.SaveChangesAsync();
			}

			// 5. Hủy vé tạm (Xóa ExternalCookie)
			await HttpContext.SignOutAsync("ExternalCookie");

			// 6. Cấp vé chính thức vào hệ thống
			await SignInUserAsync(user);

			// 7. Hiển thị lời chào
			if (isNewUser)
			{
				TempData["SuccessMessage"] = $"Chào mừng {user.FullName}! Tài khoản của bạn đã được tạo thành công.";
			}
			else
			{
				TempData["SuccessMessage"] = $"Đăng nhập thành công, chào mừng trở lại!";
			}

			return LocalRedirect(returnUrl);
		}

		/// <summary>
		/// Hàm Helper: Cấp Cookie chính thức cho hệ thống
		/// </summary>
		private async Task SignInUserAsync(Users user)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
				new Claim(ClaimTypes.Name, user.FullName),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			};

			// Ép dữ liệu Avatar vào Claim để hiện lên góc màn hình (nếu muốn)
			if (!string.IsNullOrEmpty(user.AvatarPath))
			{
				claims.Add(new Claim("AvatarPath", user.AvatarPath));
			}

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
			};

			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(claimsIdentity),
				authProperties);
		}
	}
}
