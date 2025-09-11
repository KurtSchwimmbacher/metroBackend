using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;
using metroApi.Models.DTOs;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubcategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubcategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Subcategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubcategoryDto>>> GetSubcategories()
        {
            try
            {
                var subcategories = await _context.Subcategories
                    .Include(s => s.Category)
                    .Select(s => new SubcategoryDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        CategoryId = s.CategoryId,
                        CategoryName = s.Category != null ? s.Category.Name : string.Empty
                    })
                    .ToListAsync();

                return Ok(subcategories);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching subcategories: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching subcategories");
            }
        }

        // GET: api/Subcategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Subcategory>> GetSubcategory(int id)
        {
            var subcategory = await _context.Subcategories
                .Include(s => s.Category)
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subcategory == null)
            {
                return NotFound();
            }

            return subcategory;
        }

        // POST: api/Subcategories
        [HttpPost]
        public async Task<ActionResult<Subcategory>> CreateSubcategory([FromBody] Subcategory subcategory)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Subcategories.Add(subcategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubcategory), new { id = subcategory.Id }, subcategory);
        }

        // PUT: api/Subcategories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubcategory(int id, [FromBody] Subcategory subcategory)
        {
            if (id != subcategory.Id || !ModelState.IsValid)
            {
                return BadRequest();
            }

            _context.Entry(subcategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubcategoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Subcategories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubcategory(int id)
        {
            var subcategory = await _context.Subcategories.FindAsync(id);
            if (subcategory == null)
            {
                return NotFound();
            }

            _context.Subcategories.Remove(subcategory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SubcategoryExists(int id)
        {
            return _context.Subcategories.Any(e => e.Id == id);
        }
    }
}