using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using ContactPro.Enums;
using Microsoft.AspNetCore.Identity;
using ContactPro.Services.Interfaces;
using ContactPro.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ContactPro.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;
using Org.BouncyCastle.Crypto.Modes;

namespace ContactPro.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;
        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        [Authorize]
        public IActionResult Index(string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            List<Contact> contacts = new List<Contact>();

            string? userId = _userManager.GetUserId(User);

            AppUser appUser = _context.Users.Include(c => c.Contacts)
                                            .ThenInclude(c => c.Categories)
                                            .FirstOrDefault(u => u.Id == userId)!;

            var categories = appUser.Categories;

            ViewData["CategoryId"] = new SelectList(categories,"Id","Name");
            
            contacts = appUser?.Contacts.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList()!;

            return View(contacts);
        }

        [ActionName("Index")]
        [HttpPost]
        public async Task<IActionResult> Index2(int categoryId)
        {
            List<Contact> contacts = new List<Contact>();

            string? userId = _userManager.GetUserId(User);

            AppUser appUser = _context.Users.Include(c => c.Contacts)
                                            .ThenInclude(c => c.Categories)
                                            .FirstOrDefault(u => u.Id == userId)!;

            var categories = appUser.Categories;

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name",categoryId);

            if (categoryId == 0)
            {
                contacts = appUser?.Contacts.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList()!;
            }
            else
            {
                contacts = appUser.Categories.FirstOrDefault(c => c.Id == categoryId)
                                             .Contacts.OrderBy(c => c.LastName)
                                             .ThenBy(c => c.FirstName)
                                             .ToList()!;
            }

            return View(contacts);
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EmailContact(int id)
        {
            EmailContactViewModel emailContact = new EmailContactViewModel();

            string appUserId = _userManager.GetUserId(User)!;
            Contact? contact = await _context.Contacts.Where(c => c.Id == id && c.AppUserId == appUserId)
                                                      .FirstOrDefaultAsync();
            if (contact == null)
            {
                return NotFound();
            }

            EmailData emailData = new EmailData()
            {
                EmailAddress = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            emailContact.Contact = contact;
            emailContact.EmailData = emailData;

            
            return View(emailContact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactViewModel ecvmodel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _emailService.SendEmailAsync(ecvmodel.EmailData.EmailAddress, ecvmodel.EmailData.Subject, ecvmodel.EmailData.Body);
                    return RedirectToAction(nameof(Index),"Contacts", new {swalMessage = "Success: Email Sent!"});
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(Index),"Contacts", new { swalMessage = "Error: Email Send Fail!" });

                    throw;
                }
            }
            return View(ecvmodel);
        }
    
        // GET: Contacts/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            string? appUserId = _userManager.GetUserId(User);
            
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId),"Id","Name");

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());

            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, List<int> CategoryList)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);
                contact.Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

                if (contact.BirthDate != null)
                {
                    contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.DateTime,DateTimeKind.Utc);
                }

                if (contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ContentType = contact.ImageFile.ContentType;       
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                foreach (var categoryId in CategoryList)
                {
                    await _addressBookService.AddContactToCategoryAsync(categoryId,contact.Id);
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Contacts/Edit/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            string? appUserId = _userManager.GetUserId(User);

            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts.Where(c => c.Id == id && c.AppUserId == appUserId)
                                                 .FirstOrDefaultAsync();
            if (contact == null)
            {
                return NotFound();
            }
            var stateList = Enum.GetValues(typeof(States)).Cast<States>().ToList();
            
            List<Category> categories = (await _addressBookService.GetUserCategoriesAsync(appUserId)).ToList();

            ViewData["StatesList"] = new SelectList(stateList,contact.State);

            ViewData["CategoryList"] = new MultiSelectList(categories,"Id","Name",await _addressBookService.GetContactCategoryIdsAsync(contact.Id));

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserId,FirstName,LastName,BirthDate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageData,ContentType,ImageFile")] Contact contact, List<int> categoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.Created = DateTime.SpecifyKind(contact.Created.DateTime, DateTimeKind.Utc);

                    if (contact.BirthDate != default(DateTimeOffset))
                    {
                        contact.BirthDate = DateTime.SpecifyKind(contact.BirthDate.DateTime, DateTimeKind.Utc);
                    }

                    if (contact.ImageFile is not null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ContentType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    /**save our categories**/

                    //remove the current categories
                    List<Category> oldCategories = (await _addressBookService.GetContactCategoriesAsync(contact.Id)).ToList();

                    foreach (var category in oldCategories)
                    {
                        await _addressBookService.RemoveContactFromCategoryAsync(contact.Id,category.Id);
                    }
                    //add the selected categories

                    foreach (int categoryId in categoryList)
                    {
                        await _addressBookService.AddContactToCategoryAsync(categoryId,contact.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            string? appUserId = _userManager.GetUserId(User);

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == appUserId);
            
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string? appUserId = _userManager.GetUserId(User);

            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.AppUserId == appUserId);

            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult SearchContacts(string searchString)
        {
            List<Contact> contacts = new();

            string? userId = _userManager.GetUserId(User);

            AppUser appUser = _context.Users
                                      .Include(u => u.Contacts)
                                      .ThenInclude(c => c.Categories)
                                      .FirstOrDefault(u => u.Id == userId)!;

            var categories = appUser.Categories;

            if (string.IsNullOrEmpty(searchString))
            {
                contacts = appUser.Contacts.OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToList();
            }
            else
            {
                contacts = appUser.Contacts.Where(c => c.FullName.ToLower().Contains(searchString.ToLower()))
                                                                           .OrderBy(c => c.FirstName)
                                                                           .ThenBy(c => c.LastName).ToList();
                                           
            }
            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
            
            
            return View(nameof(Index),contacts);
        }
        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
