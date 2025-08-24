using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users/firebaseUserId
        [HttpGet("{firebaseUserId}")]
        public async Task<ActionResult<User>> GetUser(string firebaseUserId)
        {
            var user = await _context.Users
                .Include(u => u.Cart)
                .ThenInclude(c => c!.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> CreateOrUpdateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUserId == user.FirebaseUserId);

            if (existingUser != null)
            {
                existingUser.Email = user.Email;
                existingUser.FullName = user.FullName;
                // Update other fields as needed
                await _context.SaveChangesAsync();
                return Ok(existingUser);
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { firebaseUserId = user.FirebaseUserId }, user);
        }

        // PUT: api/Users/firebaseUserId
        [HttpPut("{firebaseUserId}")]
        public async Task<IActionResult> UpdateUser(string firebaseUserId, [FromBody] User user)
        {
            if (firebaseUserId != user.FirebaseUserId || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);

            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.Email = user.Email;
            existingUser.FullName = user.FullName;
            // Update other fields as needed

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(firebaseUserId))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Users/firebaseUserId
        [HttpDelete("{firebaseUserId}")]
        public async Task<IActionResult> DeleteUser(string firebaseUserId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(string firebaseUserId)
        {
            return _context.Users.Any(e => e.FirebaseUserId == firebaseUserId);
        }
    }
}