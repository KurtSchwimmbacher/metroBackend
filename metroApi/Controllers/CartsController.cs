using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Firebase")]
    public class CartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Carts/userId
        [HttpGet("{userId}")]
        public async Task<ActionResult<Cart>> GetCart(int userId)
        {
            // TODO: Validate that userId matches Firebase authenticated user (User.FindFirstValue(ClaimTypes.NameIdentifier))
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

        // POST: api/Carts/add-item
        [HttpPost("add-item")]
        public async Task<ActionResult<CartItem>> AddItemToCart([FromBody] CartItem item)
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

            return CreatedAtAction(nameof(GetCart), new { userId = cart.UserId }, item);
        }

        // PUT: api/Carts/update-item
        [HttpPut("update-item")]
        public async Task<IActionResult> UpdateCartItem([FromBody] CartItem item)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == item.Id && ci.CartId == item.CartId);

            if (existingItem == null)
            {
                return NotFound();
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
        public async Task<IActionResult> RemoveCartItem(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
            {
                return NotFound();
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