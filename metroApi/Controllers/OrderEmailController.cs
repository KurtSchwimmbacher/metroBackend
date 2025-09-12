using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace metroApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderEmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OrderEmailController> _logger;

        public OrderEmailController(IConfiguration configuration, HttpClient httpClient, ILogger<OrderEmailController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        // POST: api/OrderEmail/send-order-confirmation
        [HttpPost("send-order-confirmation")]
        public async Task<IActionResult> SendOrderConfirmation([FromBody] OrderEmailRequest request)
        {
            try
            {
                var emailConfig = _configuration.GetSection("EmailJS");
                var publicKey = emailConfig["PublicKey"];
                var serviceId = emailConfig["ServiceId"];
                var templateId = emailConfig["TemplateId"];
                var apiUrl = emailConfig["ApiUrl"];

                if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(serviceId) || 
                    string.IsNullOrEmpty(templateId) || string.IsNullOrEmpty(apiUrl))
                {
                    return BadRequest("EmailJS configuration is missing");
                }

                // Send email to customer
                var customerEmailSent = await SendEmailAsync(
                    apiUrl, publicKey, serviceId, templateId, 
                    request.CustomerEmail, request.CustomerName, request.OrderData);

                // Send email to admin
                var adminEmailSent = await SendEmailAsync(
                    apiUrl, publicKey, serviceId, templateId, 
                    "admin@metro.com", "Admin", request.OrderData);

                if (customerEmailSent && adminEmailSent)
                {
                    return Ok(new { message = "Order confirmation emails sent successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to send one or more emails" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation emails");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private async Task<bool> SendEmailAsync(string apiUrl, string publicKey, string serviceId, 
            string templateId, string toEmail, string toName, OrderData orderData)
        {
            try
            {
                var emailData = new
                {
                    service_id = serviceId,
                    template_id = templateId,
                    user_id = publicKey,
                    template_params = new
                    {
                        to_email = toEmail,
                        to_name = toName,
                        order_id = orderData.OrderId,
                        email = orderData.CustomerEmail,
                        orders = orderData.Orders.Select(o => new
                        {
                            name = o.Name,
                            units = o.Units,
                            price = o.Price
                        }).ToArray(),
                        cost = new
                        {
                            shipping = orderData.Cost.Shipping,
                            tax = orderData.Cost.Tax,
                            total = orderData.Cost.Total
                        }
                    }
                };

                var json = JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(apiUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }
    }

    public class OrderEmailRequest
    {
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public OrderData OrderData { get; set; } = new();
    }

    public class OrderData
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public List<OrderItem> Orders { get; set; } = new();
        public OrderCost Cost { get; set; } = new();
    }

    public class OrderItem
    {
        public string Name { get; set; } = string.Empty;
        public int Units { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderCost
    {
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
}
