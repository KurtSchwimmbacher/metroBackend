using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CartItems/cartId?firebaseUserId=xyz
        [HttpGet("cart/{cartId}")]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCartItems(int cartId, [FromQuery] string firebaseUserId)
        {
            // Validate user ownership
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart == null)
            {
                return NotFound("Cart not found");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || cart.UserId != user.Id)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
        }

        // GET: api/CartItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItem>> GetCartItem(int id, [FromQuery] string firebaseUserId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id);

            if (cartItem == null)
            {
                return NotFound();
            }

            // Validate user ownership
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || cartItem.Cart!.UserId != user.Id)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            return cartItem;
        }

        // POST: api/CartItems
        [HttpPost]
        public async Task<ActionResult<CartItem>> CreateCartItem([FromBody] CartItem cartItem, [FromQuery] string firebaseUserId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartItem.CartId);
            if (cart == null)
            {
                return NotFound("Cart not found");
            }

            // Validate user ownership
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || cart.UserId != user.Id)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCartItem), new { id = cartItem.Id, firebaseUserId }, cartItem);
        }

        // PUT: api/CartItems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] CartItem cartItem, [FromQuery] string firebaseUserId)
        {
            if (id != cartItem.Id || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var existingItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id);

            if (existingItem == null)
            {
                return NotFound();
            }

            // Validate user ownership
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || existingItem.Cart!.UserId != user.Id)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            existingItem.Quantity = cartItem.Quantity;
            // Update other fields as needed

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartItemExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/CartItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id, [FromQuery] string firebaseUserId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == id);

            if (cartItem == null)
            {
                return NotFound();
            }

            // Validate user ownership
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || cartItem.Cart!.UserId != user.Id)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CartItemExists(int id)
        {
            return _context.CartItems.Any(e => e.Id == id);
        }
    }
}