namespace metroApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "LED UFO High Bay Color Selectable - 240W"
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Applications { get; set; } = new List<string>();
        public List<string> Advantages { get; set; } = new List<string>();
        public string ImageUrl { get; set; } = string.Empty; // URL to image (e.g., from Firebase Storage)

        public int SubcategoryId { get; set; } // Foreign key
        public Subcategory? Subcategory { get; set; } // Navigation property
    }
}