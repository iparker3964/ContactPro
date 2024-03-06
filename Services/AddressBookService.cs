using ContactPro.Data;
using ContactPro.Models;
using ContactPro.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ContactPro.Services
{
    public class AddressBookService : IAddressBookService
    {
        private readonly ApplicationDbContext _context;

        public AddressBookService(ApplicationDbContext context)
        {
            _context = context;    
        }
        public async Task AddContactToCategoryAsync(int categoryId, int contactId)
        {
            try
            {
                bool hasCategory = IsContactInCategory(categoryId, contactId).Result;

                if (hasCategory == false)
                {
                    Contact? contact = await _context.Contacts.FindAsync(contactId);

                    if (contact != null)
                    {
                        Category? category = await _context.Categories.FindAsync(categoryId);

                        if (category != null)
                        {
                            category.Contacts.Add(contact);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** Error adding contact to category");
                Console.WriteLine(ex.Message);

                throw;
            }
        }


        public async Task<ICollection<Category>> GetContactCategoriesAsync(int contactId)
        {
            try
            {
                Contact? contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c => c.Id == contactId);

                return contact.Categories;
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** Error getting contact categories ***");
                Console.WriteLine(ex.Message);
                Console.WriteLine("****************************************");

                throw;
            }
        }

        public async Task<ICollection<int>> GetContactCategoryIdsAsync(int contactId)
        {
            try
            {
                List<int> categoryIds = new List<int>();

                var contact = await _context.Contacts.Include(c => c.Categories)
                                                     .FirstOrDefaultAsync(c => c.Id == contactId);

                categoryIds = contact.Categories.Select(c => c.Id).ToList();

                return categoryIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine("**** Error getting contact categories ****");
                Console.WriteLine(ex.Message);
                Console.WriteLine("******************************************");

                throw;
            }
           
        }
        public async Task<IEnumerable<Category>> GetUserCategoriesAsync(string userId)
        {
            List<Category> categories = new List<Category>();
            
            try
            {
                categories = await _context.Categories.Where(x => x.AppUserId == userId)
                                                      .OrderBy(x => x.Name)
                                                      .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** Error getting user categories ***");
                Console.WriteLine(ex.Message);

                throw;
            }

            return categories;

        }
        public async Task<bool> IsContactInCategory(int categoryId, int contactId)
        {
            bool hasCategory = false;

            try
            {
                Contact? contact = await _context.Contacts.FindAsync(contactId);

                if (contact != null)
                {
                    hasCategory = await _context.Categories.Include(c => c.Contacts)
                                                     .Where(c => c.Id == categoryId && c.Contacts.Contains(contact))
                                                     .AnyAsync();
                }

                return hasCategory;
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** Error checking if contact is in category ***");
                Console.WriteLine(ex.Message);

                throw;
            }
        }

        public async Task RemoveContactFromCategoryAsync(int contactId, int categoryId)
        {
            try
            {

                if (await IsContactInCategory(categoryId,contactId))
                {
                    Contact? contact = await _context.Contacts.FindAsync(contactId);
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if (contact != null && category != null)
                    {
                        category.Contacts.Remove(contact);

                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** ERROR ****");
                Console.WriteLine(ex.Message);
                Console.WriteLine("**************");

                throw;
            }
        }

        public IEnumerable<Contact> SearchForContact(string searchTerm, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
