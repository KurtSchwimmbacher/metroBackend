using System.Text;
using System.Text.Json;

namespace metroApi.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(HttpClient httpClient, IConfiguration configuration, ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            try
            {
                var apiKey = _configuration["Brevo:ApiKey"];
                var fromEmail = _configuration["Brevo:FromEmail"];
                var fromName = _configuration["Brevo:FromName"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("Brevo configuration is missing");
                    return false;
                }

                var url = "https://api.brevo.com/v3/smtp/email";
                
                var emailData = new
                {
                    sender = new
                    {
                        email = fromEmail,
                        name = fromName ?? "Metro Store"
                    },
                    to = new[]
                    {
                        new
                        {
                            email = toEmail,
                            name = toName
                        }
                    },
                    subject = subject,
                    htmlContent = htmlContent
                };

                var json = JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // Add API key header
                request.Headers.Add("api-key", apiKey);

                _logger.LogInformation("Sending email via Brevo to {Email}", toEmail);
                _logger.LogDebug("Brevo payload: {Payload}", json);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Brevo API call failed. Status: {StatusCode}, Content: {Content}", 
                        response.StatusCode, errorContent);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Email sent successfully to {Email}. Response: {Response}", toEmail, responseContent);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via Brevo to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationAsync(string customerEmail, string customerName, OrderData orderData)
        {
            var subject = $"Order Confirmation - {orderData.OrderId}";
            var htmlContent = GenerateOrderConfirmationHtml(orderData);
            
            return await SendEmailAsync(customerEmail, customerName, subject, htmlContent);
        }

        public async Task<bool> SendAdminNotificationAsync(string adminEmail, string adminName, OrderData orderData)
        {
            var subject = $"New Order Notification - {orderData.OrderId}";
            var htmlContent = GenerateAdminNotificationHtml(orderData);
            
            return await SendEmailAsync(adminEmail, adminName, subject, htmlContent);
        }

        private string GenerateOrderConfirmationHtml(OrderData orderData)
        {
            var ordersHtml = string.Join("", orderData.Orders.Select(order => $@"
                <tr style=""vertical-align: top;"">
                    <td style=""padding: 24px 8px 0px; width: 78.103%;"">
                        <div>{order.Name}</div>
                        <div style=""font-size: 14px; color: #888; padding-top: 4px;"">QTY: {order.Units}</div>
                    </td>
                    <td style=""padding: 24px 4px 0px 0px; white-space: nowrap; width: 11.6361%;""><strong>${order.Price}</strong></td>
                </tr>
            "));

            return $@"
                <div style=""font-family: system-ui, sans-serif, Arial; font-size: 14px; color: #333; padding: 14px 8px; background-color: #f5f5f5;"">
                    <div style=""max-width: 600px; margin: auto; background-color: #fff;"">
                        <div style=""border-top: 6px solid #458500; padding: 16px;"">
                            <span style=""font-size: 16px; vertical-align: middle; border-left: 1px solid #333; padding-left: 8px;""><strong>Thank You for Your Order</strong></span>
                        </div>
                        <div style=""padding: 0 16px;"">
                            <p>We'll send you tracking information when the order ships.</p>
                            <div style=""text-align: left; font-size: 14px; padding-bottom: 4px; border-bottom: 2px solid #333;""><strong>Order # {orderData.OrderId}</strong></div>
                            <br>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tbody>
                                    {ordersHtml}
                                </tbody>
                            </table>
                            <div style=""padding: 24px 0;"">
                                <div style=""border-top: 2px solid #333;"">&nbsp;</div>
                            </div>
                            <table style=""border-collapse: collapse; width: 100%; text-align: right;"">
                                <tbody>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td>Shipping</td>
                                        <td style=""padding: 8px; white-space: nowrap;"">${orderData.Cost.Shipping}</td>
                                    </tr>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td>Taxes</td>
                                        <td style=""padding: 8px; white-space: nowrap;"">${orderData.Cost.Tax}</td>
                                    </tr>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td style=""border-top: 2px solid #333;""><strong style=""white-space: nowrap;"">Order Total</strong></td>
                                        <td style=""padding: 16px 8px; border-top: 2px solid #333; white-space: nowrap;""><strong>${orderData.Cost.Total}</strong></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div style=""max-width: 600px; margin: auto;"">
                            <p style=""color: #999;"">The email was sent to {orderData.CustomerEmail}<br>You received this email because you placed the order</p>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateAdminNotificationHtml(OrderData orderData)
        {
            var ordersHtml = string.Join("", orderData.Orders.Select(order => $@"
                <tr style=""vertical-align: top;"">
                    <td style=""padding: 24px 8px 0px; width: 78.103%;"">
                        <div>{order.Name}</div>
                        <div style=""font-size: 14px; color: #888; padding-top: 4px;"">QTY: {order.Units}</div>
                    </td>
                    <td style=""padding: 24px 4px 0px 0px; white-space: nowrap; width: 11.6361%;""><strong>${order.Price}</strong></td>
                </tr>
            "));

            return $@"
                <div style=""font-family: system-ui, sans-serif, Arial; font-size: 14px; color: #333; padding: 14px 8px; background-color: #f5f5f5;"">
                    <div style=""max-width: 600px; margin: auto; background-color: #fff;"">
                        <div style=""border-top: 6px solid #ff6b35; padding: 16px;"">
                            <span style=""font-size: 16px; vertical-align: middle; border-left: 1px solid #333; padding-left: 8px;""><strong>New Order Received</strong></span>
                        </div>
                        <div style=""padding: 0 16px;"">
                            <p><strong>Customer:</strong> {orderData.CustomerEmail}</p>
                            <div style=""text-align: left; font-size: 14px; padding-bottom: 4px; border-bottom: 2px solid #333;""><strong>Order # {orderData.OrderId}</strong></div>
                            <br>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tbody>
                                    {ordersHtml}
                                </tbody>
                            </table>
                            <div style=""padding: 24px 0;"">
                                <div style=""border-top: 2px solid #333;"">&nbsp;</div>
                            </div>
                            <table style=""border-collapse: collapse; width: 100%; text-align: right;"">
                                <tbody>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td>Shipping</td>
                                        <td style=""padding: 8px; white-space: nowrap;"">${orderData.Cost.Shipping}</td>
                                    </tr>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td>Taxes</td>
                                        <td style=""padding: 8px; white-space: nowrap;"">${orderData.Cost.Tax}</td>
                                    </tr>
                                    <tr>
                                        <td style=""width: 60%;"">&nbsp;</td>
                                        <td style=""border-top: 2px solid #333;""><strong style=""white-space: nowrap;"">Order Total</strong></td>
                                        <td style=""padding: 16px 8px; border-top: 2px solid #333; white-space: nowrap;""><strong>${orderData.Cost.Total}</strong></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            ";
        }
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
