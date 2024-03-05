using ContactPro.Models;

namespace ContactPro.Services.Interfaces
{
    public interface IAddressBookService
    {
        Task AddContactToCategoryAsync(int categoryId, int contactId);
        Task<bool> IsContactInCategory(int categoryId, int contactId);
        Task<ICollection<int>> GetContactCategoryIdsAsync(int contactId);
        Task<ICollection<Category>> GetContactCategoriesAsync(int contactId);
        Task<IEnumerable<Category>> GetUserCategoriesAsync(string userId);
        Task RemoveContactFromCategoryAsync(int contactId, int categoryId);
        IEnumerable<Contact> SearchForContact(string searchTerm, string userId);
    }
}
