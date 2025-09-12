using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Carts/userId?firebaseUserId=xyz
        [HttpGet("{userId}")]
        public async Task<ActionResult<Cart>> GetCart(int userId, [FromQuery] string firebaseUserId)
        {
            // Validate that userId matches the Firebase UID
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null || user.Id != userId)
            {
                return Unauthorized("Invalid user or Firebase UID");
            }

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return NotFound();
            }

            return cart;
        }

        // POST: api/Carts
        [HttpPost]
        public async Task<ActionResult<Cart>> CreateCart([FromBody] Cart cart, [FromQuery] string firebaseUserId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate user ownership
            var user = await _context.Users.FirstOrDefaultAsync(u => u.FirebaseUserId == firebaseUserId);
            if (user == null)
            {
                return Unauthorized("Invalid Firebase UID");
            }

            // Check if user already has a cart
            var existingCart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (existingCart != null)
            {
                return Conflict("User already has a cart");
            }

            cart.UserId = user.Id;
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCart), new { userId = cart.UserId, firebaseUserId }, cart);
        }

        // POST: api/Carts/add-item
        [HttpPost("add-item")]
        public async Task<ActionResult<CartItem>> AddItemToCart([FromBody] CartItem item, [FromQuery] string firebaseUserId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == item.CartId);
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

            // Check if item already exists in cart
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == item.CartId && ci.ProductId == item.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                _context.CartItems.Add(item);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCart), new { userId = cart.UserId, firebaseUserId }, item);
        }

        // PUT: api/Carts/update-item
        [HttpPut("update-item")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartItem item, [FromQuery] string firebaseUserId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == item.Id && ci.CartId == item.CartId);

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

            existingItem.Quantity = item.Quantity;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CartItemExists(item.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Carts/remove-item/{cartItemId}
        [HttpDelete("remove-item/{cartItemId}")]
        public async Task<IActionResult> RemoveCartItem(int cartItemId, [FromQuery] string firebaseUserId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

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