using EcomerceBE.Data;
using EcomerceBE.Service.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/payment/momo")]
    public class MoMoPaymentController : ControllerBase
    {
        private readonly IMoMoPaymentService _momoPaymentService;
        private readonly AppDbContext _context;
        private readonly ILogger<MoMoPaymentController> _logger;

        public MoMoPaymentController(
            IMoMoPaymentService momoPaymentService,
            AppDbContext context,
            ILogger<MoMoPaymentController> logger)
        {
            _momoPaymentService = momoPaymentService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreateMoMoPaymentRequest request)
        {
            try
            {
                if (request == null || request.OrderId <= 0 || request.Amount <= 0)
                {
                    return BadRequest(new { message = "Invalid payment request." });
                }

                // Verify order exists and belongs to user
                var userIdClaim = User.FindFirst("id")
                    ?? User.FindFirst("Id")
                    ?? User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.UserId == userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found." });
                }

                if (order.PaymentStatus == "Completed")
                {
                    return BadRequest(new { message = "Order has already been paid." });
                }

                // Get base URL from configuration or request
                var baseUrl = Request.Scheme + "://" + Request.Host;
                var returnUrl = $"{baseUrl}/api/payment/momo/return?orderId={request.OrderId}";
                var notifyUrl = $"{baseUrl}/api/payment/momo/notify";

                // Create payment request
                var paymentRequest = new MoMoPaymentRequest
                {
                    OrderId = order.OrderNumber ?? order.OrderId.ToString(),
                    Amount = (long)request.Amount,
                    OrderInfo = $"Thanh toan don hang {order.OrderNumber ?? order.OrderId.ToString()}",
                    ReturnUrl = returnUrl,
                    NotifyUrl = notifyUrl,
                    RequestType = "captureWallet"
                };

                var paymentResponse = await _momoPaymentService.CreatePaymentRequestAsync(paymentRequest);

                // Update order with payment request ID
                order.PaymentStatus = "Pending";
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    payUrl = paymentResponse.PayUrl,
                    qrCodeUrl = paymentResponse.QrCodeUrl,
                    orderId = order.OrderId,
                    orderNumber = order.OrderNumber,
                    amount = paymentResponse.Amount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MoMo payment");
                return StatusCode(500, new { message = "Failed to create payment request.", error = ex.Message });
            }
        }

        [HttpPost("notify")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentNotify([FromBody] MoMoCallbackData callbackData)
        {
            try
            {
                _logger.LogInformation("MoMo Payment Callback received: {Data}", System.Text.Json.JsonSerializer.Serialize(callbackData));

                // Verify callback signature
                if (!_momoPaymentService.VerifyPaymentCallback(callbackData))
                {
                    _logger.LogWarning("Invalid MoMo callback signature");
                    return BadRequest(new { message = "Invalid signature" });
                }

                // Find order by OrderNumber or OrderId
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => 
                        (o.OrderNumber != null && o.OrderNumber == callbackData.OrderId) ||
                        o.OrderId.ToString() == callbackData.OrderId);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for MoMo callback: {OrderId}", callbackData.OrderId);
                    return NotFound(new { message = "Order not found" });
                }

                // Update order payment status
                if (callbackData.ResultCode == 0) // Success
                {
                    order.PaymentStatus = "Completed";
                    order.OrderStatus = order.OrderStatus == "Pending" ? "Confirmed" : order.OrderStatus;
                    
                    // Log status change
                    _context.OrderStatusLogs.Add(new Models.OrderStatusLog
                    {
                        OrderId = order.OrderId,
                        PreviousStatus = order.OrderStatus,
                        NewStatus = order.OrderStatus,
                        PreviousPaymentStatus = "Pending",
                        NewPaymentStatus = "Completed",
                        ActionType = "MoMo Payment",
                        Notes = $"MoMo payment successful. TransId: {callbackData.TransId}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    order.PaymentStatus = "Failed";
                    _logger.LogWarning("MoMo payment failed for order {OrderId}: {Message}", order.OrderId, callbackData.Message);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Callback processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo payment callback");
                return StatusCode(500, new { message = "Failed to process callback" });
            }
        }

        [HttpGet("return")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentReturn([FromQuery] string? orderId, [FromQuery] int? resultCode)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest(new { message = "OrderId is required" });
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => 
                        (o.OrderNumber != null && o.OrderNumber == orderId) ||
                        o.OrderId.ToString() == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                // Redirect to frontend success/failure page
                var frontendBaseUrl = Request.Headers["Referer"].ToString() 
                    ?? "http://localhost:3000"; // Fallback

                if (resultCode == 0)
                {
                    return Redirect($"{frontendBaseUrl}/cart/checkout/success?orderId={order.OrderId}&method=momo&status=success");
                }
                else
                {
                    return Redirect($"{frontendBaseUrl}/cart/checkout/success?orderId={order.OrderId}&method=momo&status=failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo payment return");
                return StatusCode(500, new { message = "Failed to process return" });
            }
        }

        public class CreateMoMoPaymentRequest
        {
            public int OrderId { get; set; }
            public decimal Amount { get; set; }
        }
    }
}

