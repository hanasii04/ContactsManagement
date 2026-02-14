using ContactsManagement.Interfaces;
using ContactsManagement.Models;
using ContactsManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace ContactsManagement
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews();

			builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddCookie(options =>
			{
				options.LoginPath = "/Auth/Login"; // Nếu chưa đăng nhập thì chuyển hướng về đây
				options.ExpireTimeSpan = TimeSpan.FromDays(7); // Cookie sống trong 7 ngày
				options.AccessDeniedPath = "/Auth/AccessDenied"; // Nếu không đủ quyền
			})
			.AddCookie("ExternalCookie")
			.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
			{
				// gg trả kết quả vào ExternalCookie
				options.SignInScheme = "ExternalCookie";

				options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
				options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
				options.CallbackPath = "/signin-google";

				// Lấy thêm thông tin profile và picture
				options.Scope.Add("profile");
				options.Scope.Add("email");
				options.ClaimActions.MapJsonKey("picture", "picture");
				options.SaveTokens = true;
			})
			.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
			{
				options.SignInScheme = "ExternalCookie";

				options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
				options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
				options.CallbackPath = "/signin-facebook";

				options.Scope.Add("email");
				options.Scope.Add("public_profile");
				options.Fields.Add("name");
				options.Fields.Add("email");
				options.Fields.Add("picture");

				options.SaveTokens = true;
			});

			builder.Services.AddTransient<IEmailService, EmailService>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllerRoute(
				name: "areas",
				pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
			);

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller=Home}/{action=Index}/{id?}");

			app.Run();
		}
	}
}
