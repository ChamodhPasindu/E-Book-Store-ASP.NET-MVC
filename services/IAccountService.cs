using EBookStore.Models.DTO;
using EBookStore.Models.Entity;

namespace EBookStore.services
{
    public interface IAccountService
    {
        Task<List<User>> GetActiveCustomers();

        Task<List<OrderViewModel>> GetDeliveredOrdersAsync(DateTime? fromDate, DateTime? toDate);

        Task<bool> RegisterCustomerAsync(User newUser);

        Task<bool> RegisterAdminAsync(User newUser);

        Task<(bool isSuccess, User? user, string errorMessage)> LoginAsync(string email, string password);

        Task<User?> GetCustomerByIdAsync(int id);

        Task<bool> DeleteUserAsync(int id);

        Task<bool> UpdateUserDetailsAsync(UserSettingsViewModel model);

        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        Task<byte[]> DownloadCustomersAsync();
    }
}
