namespace EBookStore.Models.DTO
{
    public class UserSettingsViewModel
    {
        public int UserID { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Address { get; set; }
    }
}
