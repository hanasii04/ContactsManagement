using ContactsManagement.Interfaces;
using System.Net;
using System.Net.Mail;

namespace ContactsManagement.Services
{
	public class EmailService : IEmailService
	{
		private readonly IConfiguration _configuration;
		public EmailService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
		{
			try
			{
				// Lấy cấu hình từ appsettings.json
				var smtpHost = _configuration["EmailSettings:SmtpHost"];
				var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
				var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
				var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
				var fromEmail = _configuration["EmailSettings:FromEmail"];
				var fromName = _configuration["EmailSettings:FromName"];

				// Tạo mail message
				var mailMessage = new MailMessage
				{
					From = new MailAddress(fromEmail, fromName),
					Subject = "Đặt lại mật khẩu - MyContact",
					Body = GetPasswordResetEmailBody(userName, resetLink),
					IsBodyHtml = true,
					Priority = MailPriority.High
				};

				mailMessage.To.Add(toEmail);

				// Cấu hình SMTP client
				using var smtpClient = new SmtpClient(smtpHost, smtpPort)
				{
					Credentials = new NetworkCredential(smtpUsername, smtpPassword),
					EnableSsl = true,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					Timeout = 30000 // 30 seconds
				};

				// Gửi email
				await smtpClient.SendMailAsync(mailMessage);
			}
			catch (Exception ex)
			{
				// Log error (nếu có logger)
				// _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
				throw new Exception($"Không thể gửi email đến {toEmail}", ex);
			}
		}

		private string GetPasswordResetEmailBody(string userName, string resetLink)
		{
			return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đặt lại mật khẩu</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            font-family: 'Arial', 'Helvetica', sans-serif;
            line-height: 1.6;
            color: #333333;
            background-color: #f4f4f4;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .header .icon {{
            font-size: 50px;
            margin-bottom: 10px;
        }}
        .content {{
            padding: 40px 30px;
            background-color: #ffffff;
        }}
        .content p {{
            margin: 0 0 15px 0;
            font-size: 16px;
            color: #555555;
        }}
        .content strong {{
            color: #333333;
        }}
        .button-container {{
            text-align: center;
            margin: 30px 0;
        }}
        .reset-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff !important;
            padding: 15px 40px;
            text-decoration: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
            transition: all 0.3s;
        }}
        .reset-button:hover {{
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.6);
            transform: translateY(-2px);
        }}
        .info-box {{
            background-color: #fff8e1;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .info-box p {{
            margin: 0;
            font-size: 14px;
            color: #856404;
        }}
        .link-box {{
            background-color: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            word-wrap: break-word;
        }}
        .link-box p {{
            margin: 0 0 10px 0;
            font-size: 13px;
            color: #6c757d;
        }}
        .link-box a {{
            color: #667eea;
            word-break: break-all;
            font-size: 13px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            margin: 5px 0;
            font-size: 13px;
            color: #6c757d;
        }}
        .footer a {{
            color: #667eea;
            text-decoration: none;
        }}
        .divider {{
            height: 1px;
            background-color: #e9ecef;
            margin: 30px 0;
        }}
        @media only screen and (max-width: 600px) {{
            .header {{
                padding: 30px 20px;
            }}
            .header h1 {{
                font-size: 24px;
            }}
            .content {{
                padding: 30px 20px;
            }}
            .reset-button {{
                padding: 12px 30px;
                font-size: 15px;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        
        <!-- Header -->
        <div class='header'>
            <div class='icon'>🔐</div>
            <h1>Đặt lại mật khẩu</h1>
        </div>

        <!-- Content -->
        <div class='content'>
            <p>Xin chào <strong>{userName}</strong>,</p>
            
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn tại <strong>MyContact</strong>.</p>
            
            <p>Vui lòng click vào nút bên dưới để đặt lại mật khẩu:</p>

            <!-- Button -->
            <div class='button-container'>
                <a href='{resetLink}' class='reset-button'>
                    🔑 Đặt lại mật khẩu
                </a>
            </div>

            <!-- Warning Box -->
            <div class='info-box'>
                <p><strong>⚠️ Lưu ý quan trọng:</strong></p>
                <p>• Link này chỉ có hiệu lực trong <strong>15 phút</strong></p>
                <p>• Link chỉ có thể sử dụng <strong>một lần duy nhất</strong></p>
            </div>

            <p>Nếu bạn <strong>không yêu cầu</strong> đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>

            <div class='divider'></div>

            <!-- Link Box -->
            <div class='link-box'>
                <p><strong>Nếu nút bên trên không hoạt động, copy link sau vào trình duyệt:</strong></p>
                <a href='{resetLink}'>{resetLink}</a>
            </div>
        </div>

        <!-- Footer -->
        <div class='footer'>
            <p><strong>MyContact</strong> - Quản lý danh bạ thông minh</p>
            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
            <p style='margin-top: 15px;'>
                <a href='mailto:support@mycontact.com'>Liên hệ hỗ trợ</a> | 
                <a href='#'>Chính sách bảo mật</a>
            </p>
            <p style='margin-top: 20px; color: #999999; font-size: 12px;'>
                &copy; 2025 MyContact. All rights reserved.
            </p>
        </div>

    </div>
</body>
</html>";
		}
	}
}
