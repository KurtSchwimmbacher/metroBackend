using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;
using metroApi.Models.DTOs;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Subcategories)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Subcategories = c.Subcategories.Select(s => new SubcategoryDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            CategoryId = s.CategoryId,
                            CategoryName = c.Name
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching categories");
            }
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Subcategories)
                    .Where(c => c.Id == id)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Subcategories = c.Subcategories.Select(s => new SubcategoryDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            CategoryId = s.CategoryId,
                            CategoryName = c.Name
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound();
                }

                return Ok(category);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching category: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching category");
            }
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory([FromBody] Category category)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id != category.Id || !ModelState.IsValid)
            {
                return BadRequest();
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}