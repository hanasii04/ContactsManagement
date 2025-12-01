namespace ContactsManagement.Models
{
	public class ContactCategories
	{
		public int ContactId { get; set; }
		public int CategoriesId { get; set; }
		public virtual Contacts Contacts { get; set; }
		public virtual Categories Categories { get; set; }
	}
}
