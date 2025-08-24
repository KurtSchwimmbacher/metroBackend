namespace metroApi.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "Lighting"

        // Navigation property for subcategories
        public ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
    }
}