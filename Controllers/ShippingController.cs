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
    [Route("api/shipping")]
    [Authorize(Roles = "Admin")]
    public class ShippingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(AppDbContext context, ILogger<ShippingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/shipping?orderId=...&status=...&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetShippings(
            [FromQuery] int? orderId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Shippings
                .Include(s => s.Order)
                    .ThenInclude(o => o.User)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Address)
                .AsQueryable();

            if (orderId.HasValue)
            {
                query = query.Where(s => s.OrderId == orderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status != null && s.Status.ToLower() == status.ToLower());
            }

            var totalCount = await query.CountAsync();

            var shippings = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.ShippingId,
                    s.OrderId,
                    OrderNumber = s.Order.OrderNumber,
                    CustomerName = s.Order.User.Name,
                    CustomerEmail = s.Order.User.Email,
                    ShippingAddress = s.Order.Address != null
                        ? $"{s.Order.Address.Street} {s.Order.Address.Apartment} {s.Order.Address.City} {s.Order.Address.Province} {s.Order.Address.Country}".Trim()
                        : null,
                    s.TrackingNumber,
                    s.Carrier,
                    s.ShippingMethod,
                    s.Status,
                    s.ShippedDate,
                    s.EstimatedDeliveryDate,
                    s.DeliveredDate,
                    s.Notes,
                    s.CreatedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = shippings,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }

        // GET /api/shipping/order/{orderId}
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetShippingByOrderId(int orderId)
        {
            var shipping = await _context.Shippings
                .Include(s => s.Order)
                    .ThenInclude(o => o.User)
                .Include(s => s.Order)
                    .ThenInclude(o => o.Address)
                .FirstOrDefaultAsync(s => s.OrderId == orderId);

            if (shipping == null)
                return NotFound(new { message = "Shipping not found for this order." });

            return Ok(new
            {
                shippingId = shipping.ShippingId,
                orderId = shipping.OrderId,
                orderNumber = shipping.Order.OrderNumber,
                customerName = shipping.Order.User.Name,
                customerEmail = shipping.Order.User.Email,
                shippingAddress = shipping.Order.Address != null
                    ? $"{shipping.Order.Address.Street} {shipping.Order.Address.Apartment} {shipping.Order.Address.City} {shipping.Order.Address.Province} {shipping.Order.Address.Country}".Trim()
                    : null,
                trackingNumber = shipping.TrackingNumber,
                carrier = shipping.Carrier,
                shippingMethod = shipping.ShippingMethod,
                status = shipping.Status,
                shippedDate = shipping.ShippedDate,
                estimatedDeliveryDate = shipping.EstimatedDeliveryDate,
                deliveredDate = shipping.DeliveredDate,
                notes = shipping.Notes,
                createdAt = shipping.CreatedAt,
                updatedAt = shipping.UpdatedAt
            });
        }

        // POST /api/shipping
        [HttpPost]
        public async Task<IActionResult> CreateShipping([FromBody] CreateShippingDto dto)
        {
            // Check if order exists
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            // Check if shipping already exists for this order
            var existingShipping = await _context.Shippings
                .FirstOrDefaultAsync(s => s.OrderId == dto.OrderId);

            if (existingShipping != null)
                return BadRequest(new { message = "Shipping already exists for this order. Use PATCH to update." });

            // Validate order status - should be Processing or Confirmed to create shipping
            if (order.OrderStatus != "Processing" && order.OrderStatus != "Confirmed")
            {
                return BadRequest(new { message = $"Cannot create shipping for order with status '{order.OrderStatus}'. Order must be Processing or Confirmed." });
            }

            var shipping = new Shipping
            {
                OrderId = dto.OrderId,
                TrackingNumber = dto.TrackingNumber,
                Carrier = dto.Carrier,
                ShippingMethod = dto.ShippingMethod ?? "Standard",
                Status = dto.Status ?? "Pending",
                ShippedDate = dto.ShippedDate,
                EstimatedDeliveryDate = dto.EstimatedDeliveryDate,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // If shipped date is set, update order status to Shipped
            if (dto.ShippedDate.HasValue)
            {
                order.OrderStatus = "Shipped";
                shipping.Status = "In Transit";
            }

            _context.Shippings.Add(shipping);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipping created successfully.",
                shippingId = shipping.ShippingId,
                orderId = shipping.OrderId,
                orderStatus = order.OrderStatus
            });
        }

        // PATCH /api/shipping/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateShipping(int id, [FromBody] UpdateShippingDto dto)
        {
            var shipping = await _context.Shippings
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.ShippingId == id);

            if (shipping == null)
                return NotFound(new { message = "Shipping not found." });

            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
            {
                shipping.TrackingNumber = dto.TrackingNumber.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Carrier))
            {
                shipping.Carrier = dto.Carrier.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.ShippingMethod))
            {
                shipping.ShippingMethod = dto.ShippingMethod.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                shipping.Status = dto.Status.Trim();
                hasChanges = true;
            }

            if (dto.ShippedDate.HasValue)
            {
                shipping.ShippedDate = dto.ShippedDate;
                hasChanges = true;
                // Auto-update order status to Shipped if not already
                if (shipping.Order.OrderStatus != "Shipped" && shipping.Order.OrderStatus != "Delivered")
                {
                    shipping.Order.OrderStatus = "Shipped";
                }
                if (string.IsNullOrWhiteSpace(shipping.Status) || shipping.Status == "Pending")
                {
                    shipping.Status = "In Transit";
                }
            }

            if (dto.EstimatedDeliveryDate.HasValue)
            {
                shipping.EstimatedDeliveryDate = dto.EstimatedDeliveryDate;
                hasChanges = true;
            }

            if (dto.DeliveredDate.HasValue)
            {
                shipping.DeliveredDate = dto.DeliveredDate;
                shipping.Status = "Delivered";
                hasChanges = true;
                // Auto-update order status to Delivered
                if (shipping.Order.OrderStatus != "Delivered")
                {
                    shipping.Order.OrderStatus = "Delivered";
                }
            }

            if (dto.Notes != null)
            {
                shipping.Notes = dto.Notes;
                hasChanges = true;
            }

            if (!hasChanges)
                return Ok(new { message = "No changes detected.", shippingId = shipping.ShippingId });

            shipping.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipping updated successfully.",
                shippingId = shipping.ShippingId,
                orderId = shipping.OrderId,
                orderStatus = shipping.Order.OrderStatus
            });
        }

        // DELETE /api/shipping/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipping(int id)
        {
            var shipping = await _context.Shippings.FindAsync(id);

            if (shipping == null)
                return NotFound(new { message = "Shipping not found." });

            _context.Shippings.Remove(shipping);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipping deleted successfully." });
        }

        // DTOs
        public class CreateShippingDto
        {
            [Required] public int OrderId { get; set; }
            [MaxLength(100)] public string? TrackingNumber { get; set; }
            [MaxLength(100)] public string? Carrier { get; set; }
            [MaxLength(50)] public string? ShippingMethod { get; set; }
            [MaxLength(255)] public string? Status { get; set; }
            public DateTime? ShippedDate { set; get; }
            public DateTime? EstimatedDeliveryDate { get; set; }
            [MaxLength(1000)] public string? Notes { get; set; }
        }

        public class UpdateShippingDto
        {
            [MaxLength(100)] public string? TrackingNumber { get; set; }
            [MaxLength(100)] public string? Carrier { get; set; }
            [MaxLength(50)] public string? ShippingMethod { get; set; }
            [MaxLength(255)] public string? Status { get; set; }
            public DateTime? ShippedDate { get; set; }
            public DateTime? EstimatedDeliveryDate { get; set; }
            public DateTime? DeliveredDate { get; set; }
            [MaxLength(1000)] public string? Notes { get; set; }
        }
    }
}

