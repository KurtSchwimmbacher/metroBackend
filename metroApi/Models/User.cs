namespace metroApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirebaseUserId { get; set; } = string.Empty; // From Firebase Auth UID
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        // Add more fields as needed, e.g., Address, Phone, etc.

        // Navigation property for cart
        public Cart? Cart { get; set; }
    }
}