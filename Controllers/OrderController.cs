using System.ComponentModel.DataAnnotations;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(Roles = "Admin")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AppDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Order status flow validation
        private static readonly Dictionary<string, List<string>> ValidTransitions = new()
        {
            { "Pending", new List<string> { "Confirmed", "Cancelled" } },
            { "Confirmed", new List<string> { "Processing", "Cancelled" } },
            { "Processing", new List<string> { "Shipped", "Cancelled" } },
            { "Shipped", new List<string> { "Delivered", "Returned" } },
            { "Delivered", new List<string>() }, // No changes allowed
            { "Cancelled", new List<string>() }, // No changes allowed
            { "Returned", new List<string>() } // No changes allowed
        };

        private bool IsValidTransition(string currentStatus, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(newStatus))
                return false;

            currentStatus = currentStatus.Trim();
            newStatus = newStatus.Trim();

            if (currentStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                return true; // Same status is allowed

            if (!ValidTransitions.ContainsKey(currentStatus))
                return false;

            return ValidTransitions[currentStatus]
                .Any(s => s.Equals(newStatus, StringComparison.OrdinalIgnoreCase));
        }

        private async Task LogStatusChangeAsync(
            int orderId,
            string previousOrderStatus,
            string newOrderStatus,
            string? previousPaymentStatus = null,
            string? newPaymentStatus = null,
            string? actionType = "status_change",
            string? notes = null)
        {
            try
            {
                var userIdClaim = User.FindFirst("id")
                    ?? User.FindFirst("Id")
                    ?? User.FindFirst(ClaimTypes.NameIdentifier);

                int? changedByUserId = null;
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    changedByUserId = userId;

                var log = new OrderStatusLog
                {
                    OrderId = orderId,
                    PreviousStatus = previousOrderStatus,
                    NewStatus = newOrderStatus,
                    PreviousPaymentStatus = previousPaymentStatus,
                    NewPaymentStatus = newPaymentStatus,
                    ActionType = actionType,
                    Notes = notes,
                    ChangedByUserId = changedByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderStatusLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log order status change for OrderId: {OrderId}", orderId);
            }
        }

        // GET /api/orders?search=...&status=...&startDate=...&endDate=...&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .AsQueryable();

            // Search by order ID, order number, customer name, or email
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                if (int.TryParse(searchTerm, out var orderId))
                {
                    query = query.Where(o => o.OrderId == orderId);
                }
                else
                {
                    query = query.Where(o =>
                        (o.OrderNumber != null && o.OrderNumber.Contains(searchTerm)) ||
                        o.User.Name.Contains(searchTerm) ||
                        o.User.Email.Contains(searchTerm));
                }
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus.ToLower() == status.ToLower());
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDateInclusive = endDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedAt < endDateInclusive);
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get shipping info for orders
            var orderIds = orders.Select(o => o.OrderId).ToList();
            var shippings = await _context.Shippings
                .Where(s => orderIds.Contains(s.OrderId))
                .ToDictionaryAsync(s => s.OrderId, s => s);

            var ordersWithShipping = orders.Select(o => new
            {
                o.OrderId,
                OrderNumber = o.OrderNumber,
                o.UserId,
                CustomerName = o.User.Name,
                CustomerEmail = o.User.Email,
                CustomerPhone = o.Address != null ? o.Address.Phone : null,
                ShippingAddress = o.Address != null
                    ? $"{o.Address.Street} {o.Address.Apartment} {o.Address.City} {o.Address.Province} {o.Address.Country}".Trim()
                    : null,
                o.TotalAmount,
                o.DiscountAmount,
                o.PaymentMethod,
                o.PaymentStatus,
                o.OrderStatus,
                o.CreatedAt,
                Items = o.OrderItems.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.ProductId,
                    oi.Quantity,
                    oi.UnitPrice,
                    ProductName = oi.Product.Name
                }).ToList(),
                Shipping = shippings.TryGetValue(o.OrderId, out var ship) ? new
                {
                    ship.TrackingNumber,
                    ship.Carrier,
                    ship.Status,
                    ship.ShippedDate
                } : null
            }).ToList();

            return Ok(new
            {
                data = ordersWithShipping,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // GET /api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.Coupon)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            
            // Get shipping info if exists
            var shipping = await _context.Shippings
                .FirstOrDefaultAsync(s => s.OrderId == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            // Get status history
            var statusLogs = await _context.OrderStatusLogs
                .Where(log => log.OrderId == id)
                .Include(log => log.ChangedByUser)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new
                {
                    log.OrderStatusLogId,
                    log.PreviousStatus,
                    log.NewStatus,
                    log.PreviousPaymentStatus,
                    log.NewPaymentStatus,
                    log.ActionType,
                    log.Notes,
                    ChangedBy = log.ChangedByUser != null ? log.ChangedByUser.Name : "System",
                    log.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                orderId = order.OrderId,
                orderNumber = order.OrderNumber,
                userId = order.UserId,
                customerName = order.User.Name,
                customerEmail = order.User.Email,
                customerPhone = order.Address != null ? order.Address.Phone : null,
                shippingAddress = order.Address != null
                    ? $"{order.Address.Street} {order.Address.Apartment} {order.Address.City} {order.Address.Province} {order.Address.Country}".Trim()
                    : null,
                totalAmount = order.TotalAmount,
                discountAmount = order.DiscountAmount,
                paymentMethod = order.PaymentMethod,
                paymentStatus = order.PaymentStatus,
                orderStatus = order.OrderStatus,
                createdAt = order.CreatedAt,
                couponCode = order.Coupon != null ? order.Coupon.Code : null,
                items = order.OrderItems.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.ProductId,
                    oi.Quantity,
                    oi.UnitPrice,
                    productName = oi.Product.Name,
                    productImage = oi.Product.Images != null && oi.Product.Images.Any()
                        ? oi.Product.Images.First().ImageUrl
                        : null
                }).ToList(),
                statusHistory = statusLogs,
                shipping = shipping != null ? new
                {
                    shipping.ShippingId,
                    shipping.TrackingNumber,
                    shipping.Carrier,
                    shipping.ShippingMethod,
                    shipping.Status,
                    shipping.ShippedDate,
                    shipping.EstimatedDeliveryDate,
                    shipping.DeliveredDate,
                    shipping.Notes
                } : null
            });
        }

        // PATCH /api/orders/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            var previousOrderStatus = order.OrderStatus;
            var previousPaymentStatus = order.PaymentStatus;
            var hasChanges = false;

            // Validate and update OrderStatus
            if (!string.IsNullOrWhiteSpace(dto.OrderStatus))
            {
                var newStatus = dto.OrderStatus.Trim();
                if (!IsValidTransition(order.OrderStatus, newStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Invalid status transition from '{order.OrderStatus}' to '{newStatus}'.",
                        validTransitions = ValidTransitions.ContainsKey(order.OrderStatus)
                            ? ValidTransitions[order.OrderStatus]
                            : new List<string>()
                    });
                }

                if (!order.OrderStatus.Equals(newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    order.OrderStatus = newStatus;
                    hasChanges = true;
                }
            }

            // Update PaymentStatus (no strict validation, but log it)
            if (!string.IsNullOrWhiteSpace(dto.PaymentStatus))
            {
                var newPaymentStatus = dto.PaymentStatus.Trim();
                if (!order.PaymentStatus.Equals(newPaymentStatus, StringComparison.OrdinalIgnoreCase))
                {
                    order.PaymentStatus = newPaymentStatus;
                    hasChanges = true;
                }
            }

            if (!hasChanges)
                return Ok(new { message = "No changes detected.", orderId = order.OrderId });

            await _context.SaveChangesAsync();

            // Log the change
            await LogStatusChangeAsync(
                order.OrderId,
                previousOrderStatus,
                order.OrderStatus,
                previousPaymentStatus,
                order.PaymentStatus,
                "status_change",
                dto.Notes);

            return Ok(new
            {
                message = "Order status updated successfully.",
                orderId = order.OrderId,
                orderStatus = order.OrderStatus,
                paymentStatus = order.PaymentStatus
            });
        }

        // POST /api/orders/{id}/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(
            int id,
            [FromBody] CancelOrderDto? dto = null)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            // Check if order can be cancelled
            if (!IsValidTransition(order.OrderStatus, "Cancelled"))
            {
                return BadRequest(new
                {
                    message = $"Order cannot be cancelled from current status '{order.OrderStatus}'.",
                    currentStatus = order.OrderStatus
                });
            }

            var previousOrderStatus = order.OrderStatus;
            var previousPaymentStatus = order.PaymentStatus;

            order.OrderStatus = "Cancelled";
            if (order.PaymentStatus != "Failed" && order.PaymentStatus != "Refunded")
            {
                order.PaymentStatus = "Refunded"; // Auto-refund on cancel
            }

            await _context.SaveChangesAsync();

            // Log the cancellation
            await LogStatusChangeAsync(
                order.OrderId,
                previousOrderStatus,
                order.OrderStatus,
                previousPaymentStatus,
                order.PaymentStatus,
                "cancel",
                dto?.Reason ?? "Order cancelled by admin");

            return Ok(new
            {
                message = "Order cancelled successfully.",
                orderId = order.OrderId,
                orderStatus = order.OrderStatus,
                paymentStatus = order.PaymentStatus
            });
        }

        // POST /api/orders/{id}/refund
        [HttpPost("{id}/refund")]
        public async Task<IActionResult> RefundOrder(
            int id,
            [FromBody] RefundOrderDto? dto = null)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            // Only allow refund for certain statuses
            var refundableStatuses = new[] { "Delivered", "Shipped", "Processing", "Confirmed" };
            if (!refundableStatuses.Contains(order.OrderStatus, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Order cannot be refunded from current status '{order.OrderStatus}'.",
                    currentStatus = order.OrderStatus
                });
            }

            var previousPaymentStatus = order.PaymentStatus;

            order.PaymentStatus = "Refunded";
            if (order.OrderStatus != "Cancelled")
            {
                order.OrderStatus = "Returned";
            }

            await _context.SaveChangesAsync();

            // Log the refund
            await LogStatusChangeAsync(
                order.OrderId,
                order.OrderStatus,
                order.OrderStatus,
                previousPaymentStatus,
                order.PaymentStatus,
                "refund",
                dto?.Reason ?? "Order refunded by admin");

            return Ok(new
            {
                message = "Order refunded successfully.",
                orderId = order.OrderId,
                orderStatus = order.OrderStatus,
                paymentStatus = order.PaymentStatus
            });
        }

        // DTOs
        public class UpdateOrderStatusDto
        {
            public string? OrderStatus { get; set; }
            public string? PaymentStatus { get; set; }
            public string? Notes { get; set; }
        }

        public class CancelOrderDto
        {
            public string? Reason { get; set; }
        }

        public class RefundOrderDto
        {
            public string? Reason { get; set; }
        }
    }
}

