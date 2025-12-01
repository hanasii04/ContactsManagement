using Microsoft.EntityFrameworkCore;

namespace ContactsManagement.Models
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions options) : base(options)
		{
		}

		public DbSet<Users> Users { get; set; }
		public DbSet<Contacts> Contacts { get; set; }
		public DbSet<Categories> Categories { get; set; }
		public DbSet<ContactCategories> ContactCategories { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Khóa chính Tổng hợp (Composite Primary Key)
			modelBuilder.Entity<ContactCategories>()
				.HasKey(cc => new { cc.ContactId, cc.CategoriesId });

			// 1-N (Contact -> ContactCategories)
			modelBuilder.Entity<ContactCategories>()
				.HasOne(cc => cc.Contacts)
				.WithMany(c => c.ContactCategories)
				.HasForeignKey(cc => cc.ContactId);

			// 1-N (Category -> ContactCategories)
			modelBuilder.Entity<ContactCategories>()
				.HasOne(cc => cc.Categories)
				.WithMany(c => c.ContactCategories)
				.HasForeignKey(cc => cc.CategoriesId);

			// 1-N (User -> Contacts)
			modelBuilder.Entity<Users>()
				.HasMany(u => u.Contacts)
				.WithOne(c => c.Users)
				.HasForeignKey(c => c.UserId)
				.OnDelete(DeleteBehavior.Restrict); // Ngăn xóa User nếu vẫn còn Contact

			// 1-N (User -> Categories)
			modelBuilder.Entity<Users>()
				.HasMany(u => u.Categories)
				.WithOne(c => c.Users)
				.HasForeignKey(c => c.UserId)
				.OnDelete(DeleteBehavior.Restrict); // Ngăn xóa User nếu vẫn còn Category
		}
	}
}
