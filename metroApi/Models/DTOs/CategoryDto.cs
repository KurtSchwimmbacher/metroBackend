namespace metroApi.Models.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SubcategoryDto> Subcategories { get; set; } = new List<SubcategoryDto>();
    }
}
