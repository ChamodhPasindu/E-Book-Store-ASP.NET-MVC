using EBookStore.Models.DTO;

namespace EBookStore.services
{
    public interface IHomeService
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync();
    }
}
