namespace EBookStore.Models
{
    public class User
    {
        public int UserID { get; set; } 

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }

        public required string Password{ get; set; }

        public required string Role { get; set; } = "Customer"; // 'Admin' or 'Customer'

        public required string PhoneNumber { get; set; }

        public required string Address { get; set; }

        public bool IsActive { get; set; } = true; 

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}
