# 📒 Contact Pro - Personal Contact Management System

![.NET Core](https://img.shields.io/badge/.NET%20Core-8.0-purple)
![Authentication](https://img.shields.io/badge/Auth-Cookie-orange)
![AdminLTE](https://img.shields.io/badge/UI-AdminLTE%203-teal)
![Status](https://img.shields.io/badge/Status-In%20Development-orange)

**Contact Pro** là giải pháp số hóa danh bạ toàn diện được phát triển trên nền tảng ASP.NET Core MVC hiệu năng cao. Dự án không chỉ dừng lại ở việc lưu trữ thông tin, mà còn tập trung xây dựng một hệ thống phân quyền chặt chẽ (Role-based Authorization), đảm bảo tính bảo mật tuyệt đối giữa dữ liệu cá nhân của Người dùng và quyền giám sát của Quản trị viên. Với giao diện AdminLTE được tùy biến sâu, Contact Pro mang lại trải nghiệm mượt mà, hiện đại và tối ưu trên mọi thiết bị.

## 🚀 Tính năng chính 

### 👤 Phân hệ User 
* **Authentication:** Hệ thống xác thực tự xây dựng sử dụng **Cookie Authentication**
* **Dashboard cá nhân:** Thống kê tổng quan số lượng liên hệ, liên hệ mới thêm trong tháng.
* **Quản lý danh bạ:** Xem, thêm, sửa, xóa và tìm kiếm danh bạ cá nhân.
* **Hồ sơ cá nhân:** Cập nhật thông tin tài khoản.

### 🛡️ Phân hệ Admin 
* **Dashboard hệ thống:** Cái nhìn toàn cảnh về hoạt động của toàn bộ hệ thống.
* **Quản lý người dùng:** Xem danh sách user, xem chi tiết hồ sơ, khóa/mở khóa tài khoản.
* **Quản lý liên hệ hệ thống:** Admin có quyền xem danh sách tất cả liên hệ trong Database.

## 🛠️ Công nghệ sử dụng 

* **Backend:** C#, ASP.NET Core MVC, Entity Framework Core.
* **Database:** SQL Server.
* **Frontend:** Razor Views, Bootstrap 5, AdminLTE 3 Template.
* **Authentication:** Cookie Authentication

## 📸 Hình ảnh minh họa

### 1. Admin Dashboard
Giao diện dành cho quản trị viên với các thống kê về liên hệ và người dùng.
![Admin Dashboard](./docs/admin-dashboard.png
)

### 2. Admin Contact Management 
Trang quản lý danh sách liên hệ của quản trị viên.
![Admin Contact Management ](./docs/admin-contact-management.png
)

### 3. My Contact
Trang quản lý danh sách liên hệ của người dùng.
![My Contact](./docs/my-contact.png
)

### 4. Contact Details
Trang chi tiết liên hệ của người dùng.
![My Contact](./docs/contact-details.png
)

## ⚙️ Cài đặt & Chạy dự án

Để chạy dự án này trên máy cục bộ, hãy làm theo các bước sau:

1.  **Clone dự án:**
    ```bash
    git clone https://github.com/hanasii04/ContactsManagement.git
    ```

2.  **Cấu hình Database:**
    Mở file `appsettings.json` và cập nhật chuỗi kết nối `DefaultConnection` phù hợp với SQL Server của bạn.

3.  **Cập nhật Database (Migration):**
    * Nếu dùng **Visual Studio (Package Manager Console)**:
        ```powershell
        Update-Database
        ```
    * Nếu dùng **Terminal / VS Code**:
        ```bash
        dotnet ef database update
        ```

4.  **Chạy ứng dụng:**
    ```bash
    dotnet run
    ```

## 🛣️ Roadmap 

- [x] Thiết lập cấu trúc dự án & Database.
- [x] Tích hợp giao diện AdminLTE.
- [x] Chức năng Admin: Quản lý người dùng, quản lý liên hệ.
- [x] Authentication, Authorization.
- [x] User Dashboard & Layout riêng.
- [x] Chức năng User: Quản lý danh mục cá nhân.
- [x] Chức năng User: Quản lý danh bạ cá nhân.
- [ ] Chức năng User: Thay đổi thông tin cá nhân.
- [x] Nhập/xuất dữ liệu CSV.

## 🤝 Đóng góp 
Dự án này được xây dựng cho mục đích học tập. Mọi ý kiến đóng góp đều được hoan nghênh!

---
**Author:** [Hoàng]