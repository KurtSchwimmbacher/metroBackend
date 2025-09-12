using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;
using metroApi.Models.DTOs;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductsController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Subcategory)
                .ToListAsync();

            return products.Select(p => MapToDto(p)).ToList();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Subcategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return MapToDto(product);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductCreateDto productDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                Features = productDto.Features,
                Applications = productDto.Applications,
                Advantages = productDto.Advantages,
                SubcategoryId = productDto.SubcategoryId,
                ImageUrl = string.Empty // Will be set when image is uploaded
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, MapToDto(product));
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            if (id != product.Id || !ModelState.IsValid)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private ProductDto MapToDto(Product product)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = $"{request?.Scheme}://{request?.Host}";
            
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Features = product.Features,
                Applications = product.Applications,
                Advantages = product.Advantages,
                ImageUrl = !string.IsNullOrEmpty(product.ImageUrl) 
                    ? $"{baseUrl}{product.ImageUrl}" 
                    : string.Empty,
                ImageFileName = !string.IsNullOrEmpty(product.ImageUrl) 
                    ? Path.GetFileName(product.ImageUrl) 
                    : string.Empty,
                SubcategoryId = product.SubcategoryId,
                Subcategory = product.Subcategory != null ? new SubcategoryDto
                {
                    Id = product.Subcategory.Id,
                    Name = product.Subcategory.Name,
                    CategoryId = product.Subcategory.CategoryId
                } : null
            };
        }
    }
}