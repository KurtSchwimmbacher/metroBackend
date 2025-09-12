namespace metroApi.Models.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Applications { get; set; } = new List<string>();
        public List<string> Advantages { get; set; } = new List<string>();
        public string ImageUrl { get; set; } = string.Empty; // Full URL for serving
        public string ImageFileName { get; set; } = string.Empty; // Just the filename for management
        public int SubcategoryId { get; set; }
        public SubcategoryDto? Subcategory { get; set; }
    }

    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Features { get; set; } = new List<string>();
        public List<string> Applications { get; set; } = new List<string>();
        public List<string> Advantages { get; set; } = new List<string>();
        public int SubcategoryId { get; set; }
    }
}

