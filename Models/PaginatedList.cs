using Microsoft.EntityFrameworkCore;

namespace ContactsManagement.Helpers
{
	// Lớp Generic <T> giúp tái sử dụng cho mọi loại dữ liệu (ContactDTO, UserDTO,...)
	public class PaginatedList<T>
	{
		// Danh sách chứa dữ liệu thực tế của trang hiện tại
		public List<T> Items { get; set; }

		public int PageIndex { get; set; }

		public int TotalPages { get; set; }

		public int TotalCount { get; set; }

		public int PageSize { get; set; }

		public PaginatedList(List<T> items, int totalCount, int pageIndex, int pageSize)
		{
			Items = items;
			TotalCount = totalCount;
			PageIndex = pageIndex;
			PageSize = pageSize;

			// Tính tổng số trang: Lấy tổng số bản ghi chia cho số bản ghi trong 1 trang, làm tròn lên
			TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
		}

		// Trả về true nếu trang hiện tại > 1, dùng để bật tắt nút previous - next
		public bool HasPreviousPage => PageIndex > 1;
		public bool HasNextPage => PageIndex < TotalPages;

		// Hàm static dùng để thực thi query và tạo ra đối tượng PaginatedList
		// Nhận vào IQueryable (câu query chưa chạy), trang cần lấy, và kích thước trang
		public static async Task<PaginatedList<T>> CreateAsync(
			IQueryable<T> source,
			int pageIndex,
			int pageSize)
		{
			var count = await source.CountAsync();

			var items = await source
				.Skip((pageIndex - 1) * pageSize) // Bỏ qua các bản ghi của các trang trước
				.Take(pageSize)                   // Lấy đúng số lượng bản ghi của trang này
				.ToListAsync();                   

			return new PaginatedList<T>(items, count, pageIndex, pageSize);
		}
	}
}