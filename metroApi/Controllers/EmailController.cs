using Microsoft.AspNetCore.Mvc;
using metroApi.Services;
using Microsoft.Extensions.Configuration;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly BrevoEmailService _brevoService;
        private readonly ILogger<EmailController> _logger;
        private readonly IConfiguration _configuration;

        public EmailController(BrevoEmailService brevoService, ILogger<EmailController> logger, IConfiguration configuration)
        {
            _brevoService = brevoService;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: api/Email/test-config
        [HttpGet("test-config")]
        public IActionResult TestEmailConfig()
        {
            var brevoConfig = _configuration.GetSection("Brevo");
            var apiKey = brevoConfig["ApiKey"];
            var fromEmail = brevoConfig["FromEmail"];
            var fromName = brevoConfig["FromName"];

            return Ok(new
            {
                apiKey = !string.IsNullOrEmpty(apiKey) ? "✓ Set" : "✗ Missing",
                fromEmail = !string.IsNullOrEmpty(fromEmail) ? "✓ Set" : "✗ Missing",
                fromName = !string.IsNullOrEmpty(fromName) ? "✓ Set" : "✗ Missing",
                fullConfig = new
                {
                    apiKey = !string.IsNullOrEmpty(apiKey) ? "***" + apiKey.Substring(Math.Max(0, apiKey.Length - 4)) : "Not set",
                    fromEmail = fromEmail,
                    fromName = fromName
                }
            });
        }

        // POST: api/Email/send-order-confirmation
        [HttpPost("send-order-confirmation")]
        public async Task<IActionResult> SendOrderConfirmation([FromBody] OrderEmailRequest request)
        {
            try
            {
                // Send email to customer
                var customerEmailSent = await _brevoService.SendOrderConfirmationAsync(
                    request.CustomerEmail, 
                    request.CustomerName, 
                    request.OrderData
                );

                // Send email to admin
                var adminEmailSent = await _brevoService.SendAdminNotificationAsync(
                    "admin@metro.com", 
                    "Admin", 
                    request.OrderData
                );

                if (customerEmailSent && adminEmailSent)
                {
                    return Ok(new { message = "Order confirmation emails sent successfully" });
                }
                else
                {
                    var errorDetails = new List<string>();
                    if (!customerEmailSent) errorDetails.Add("Failed to send customer email");
                    if (!adminEmailSent) errorDetails.Add("Failed to send admin email");
                    
                    _logger.LogError("Email sending failed. Customer: {CustomerSent}, Admin: {AdminSent}", 
                        customerEmailSent, adminEmailSent);
                    
                    return StatusCode(500, new { 
                        message = "Failed to send one or more emails",
                        details = errorDetails
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation emails");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/Email/send-test
        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var success = await _brevoService.SendEmailAsync(
                    request.ToEmail,
                    request.ToName,
                    "Test Email from Metro API",
                    "<h1>Test Email</h1><p>This is a test email from your Metro API using Brevo.</p>"
                );

                if (success)
                {
                    return Ok(new { message = "Test email sent successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to send test email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    public class OrderEmailRequest
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public OrderData OrderData { get; set; } = new();
    }

    public class TestEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
    }
}
