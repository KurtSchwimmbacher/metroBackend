using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using metroApi.Data;
using metroApi.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageController> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageController(AppDbContext context, IWebHostEnvironment environment, ILogger<ImageController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // POST: api/Image/upload/{productId}
        [HttpPost("upload/{productId}")]
        public async Task<IActionResult> UploadProductImage(int productId, IFormFile file)
        {
            try
            {
                // Validate product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound("Product not found");
                }

                // Validate file
                var validationResult = ValidateImageFile(file);
                if (!validationResult.IsValid)
                {
                    return BadRequest(validationResult.ErrorMessage);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{productId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{fileExtension}";
                var filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update product with new image path
                var relativePath = $"/images/products/{fileName}";
                product.ImageUrl = relativePath;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Image uploaded successfully for product {ProductId}: {FilePath}", productId, relativePath);

                return Ok(new { 
                    message = "Image uploaded successfully", 
                    imageUrl = relativePath,
                    fileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for product {ProductId}", productId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Image/{fileName}
        [HttpGet("{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Image not found");
                }

                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = GetContentType(fileExtension);

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving image {FileName}", fileName);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Image/{fileName}
        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Image not found");
                }

                // Find products using this image
                var products = await _context.Products
                    .Where(p => p.ImageUrl.Contains(fileName))
                    .ToListAsync();

                // Remove image reference from products
                foreach (var product in products)
                {
                    product.ImageUrl = string.Empty;
                }
                await _context.SaveChangesAsync();

                // Delete physical file
                System.IO.File.Delete(filePath);

                _logger.LogInformation("Image deleted successfully: {FileName}", fileName);

                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {FileName}", fileName);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Image/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductImage(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound("Product not found");
                }

                if (string.IsNullOrEmpty(product.ImageUrl))
                {
                    return NotFound("Product has no image");
                }

                var fileName = Path.GetFileName(product.ImageUrl);
                return RedirectToAction(nameof(GetImage), new { fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product image for product {ProductId}", productId);
                return StatusCode(500, "Internal server error");
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "No file provided");
            }

            if (file.Length > _maxFileSize)
            {
                return (false, $"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return (false, $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Additional validation: check if file is actually an image
            // Note: System.Drawing is Windows-specific, so we'll skip this validation on other platforms
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using (var stream = file.OpenReadStream())
                    {
                        using (var image = Image.FromStream(stream))
                        {
                            // If we can create an Image object, it's valid
                        }
                    }
                }
                catch
                {
                    return (false, "File is not a valid image");
                }
            }

            return (true, string.Empty);
        }

        private string GetContentType(string fileExtension)
        {
            return fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
