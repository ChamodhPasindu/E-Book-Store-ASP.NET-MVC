using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

namespace EBookStore.services.Impl
{
    public class AccountService : IAccountService
    {
        private readonly BookStoreContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountService(BookStoreContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }


        List<User> IAccountService.GetActiveCustomers()
        {
            try
            {
                var users = _context.Users
                                    .Where(u => u.IsActive && u.Role == "Customer")
                                    .ToList();
                return users ?? new List<User>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<OrderViewModel>> GetDeliveredOrdersAsync(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
            .Include(o => o.User)
            .Where(o => o.OrderStatus == "Delivered");

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate <= toDate.Value);

            var deliveredOrders = await query.ToListAsync();

            var deliveredOrderViewModels = deliveredOrders.Select(o => new OrderViewModel
            {
                OrderID = o.OrderID,
                OrderStatus = o.OrderStatus,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                CustomerEmail = o.User.Email,
                CustomerAddress = o.User.Address,
                OrderItems = o.OrderDetails.Select(od => new OrderItemViewModel
                {
                    BookTitle = od.Book.Title,
                    Quantity = od.Quantity,
                    Price = od.Price,
                    ImageData = $"data:{od.Book.ImageMimeType};base64,{Convert.ToBase64String(od.Book.ImageData)}",
                }).ToList()
            }).ToList();

            return deliveredOrderViewModels;
        }

        public async Task<bool> RegisterCustomerAsync(User newUser)
        {
            if (newUser == null) return false;

            newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RegisterAdminAsync(User newUser)
        {
            if (newUser == null) return false;

            // Hash the password and set role to Admin
            newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);
            newUser.Role = "Admin";

            // Save admin to the database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<(bool isSuccess, User? user, string errorMessage)> LoginAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, null, "Invalid login attempt!");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Success)
            {
                return (true, user, string.Empty);
            }

            return (false, null, "Incorrect Password!");
        }

        public async Task<User?> GetCustomerByIdAsync(int id)
        {
            return await _context.Users
                .Where(b => b.UserID == id)
                .Include(b => b.FeedBacks)
                .Select(b => new User
                {
                    Role = b.Role,
                    UserID = b.UserID,
                    FirstName = b.FirstName,
                    LastName = b.LastName,
                    Email = b.Email,
                    PhoneNumber = b.PhoneNumber,
                    RegistrationDate = b.RegistrationDate,
                    Address = b.Address,
                    Password = null,
                    FeedBacks = b.FeedBacks
                }).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true; // User deleted successfully
            }
            return false; // User not found
        }

        public async Task<bool> UpdateUserDetailsAsync(UserSettingsViewModel model)
        {
            var user = await _context.Users.FindAsync(model.UserID);
            if (user == null)
            {
                return false; // User not found
            }

            // Update user details
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.Address = model.Address;

            await _context.SaveChangesAsync();
            return true; // Details updated successfully
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return false; // User not found
            }

            // Verify current password
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, currentPassword);
            if (result != PasswordVerificationResult.Success)
            {
                return false; // Current password is incorrect
            }

            // Update password
            user.Password = _passwordHasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();
            return true; // Password updated successfully
        }

        public async Task<byte[]> DownloadCustomersAsync()
        {
            var customers = await _context.Users.Where(b => b.IsActive).ToListAsync();

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf);

                document.Add(new iText.Layout.Element.Paragraph("Customers Report").SetFontSize(18).SetBold());

                var table = new iText.Layout.Element.Table(5);
                table.AddHeaderCell("Name");
                table.AddHeaderCell("Email");
                table.AddHeaderCell("Phone Number");
                table.AddHeaderCell("Address");
                table.AddHeaderCell("Registration Date");

                foreach (var customer in customers)
                {
                    table.AddCell($"{customer.FirstName} {customer.LastName}");
                    table.AddCell(customer.Email);
                    table.AddCell(customer.PhoneNumber);
                    table.AddCell(customer.Address);
                    table.AddCell(customer.RegistrationDate.ToString().Split('T')[0]);
                }
                document.Add(table);

                document.Close();

                return stream.ToArray();
            }
        }
    }
}

