using System.ComponentModel.DataAnnotations;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckOutController : ControllerBase
    {
        private readonly AppDbContext _context;
       

        public CheckOutController(AppDbContext context, ILogger<CheckOutController> logger)
        {
            _context = context;
           
        }

        // DTOs
        public class CheckoutOrderItemDto
        {
            public int ProductId { get; set; }
            public int? ProductVariantId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; } // VND tại thời điểm đặt
        }

        public class CheckoutOrderDto
        {
            [MaxLength(100)] public string? OrderNumber { get; set; } // Frontend-generated order ID
            public int? AddressId { get; set; }
            public int? ShippingMethodId { get; set; } // Selected shipping method
            public int? CouponId { get; set; }
            [Required, MaxLength(255)] public string PaymentMethod { get; set; } = "cod"; // cod|bank|vnpay|paypal
            public decimal TotalAmount { get; set; }
            public decimal DiscountAmount { get; set; }
            public List<CheckoutOrderItemDto> OrderItems { get; set; } = new();
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutOrderDto dto)
        {
            if (dto == null || dto.OrderItems.Count == 0)
                return BadRequest(new { message = "Order items are required." });

            // Lấy userId từ claims (không dùng "sub")
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            // Validate coupon nếu có
            Coupon? coupon = null;
            decimal couponDiscount = 0m;
            if (dto.CouponId.HasValue)
            {
                coupon = await _context.Coupons
                    .Include(c => c.ProductCoupons)
                    .Include(c => c.UserCoupons)
                    .Include(c => c.ShippingMethodCoupons)
                    .FirstOrDefaultAsync(c => c.CouponId == dto.CouponId.Value && c.IsActive
                        && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                        && (c.EndDate == null || c.EndDate > DateTime.UtcNow));

                if (coupon == null)
                    return BadRequest(new { message = "Invalid or expired coupon." });

                // Check usage limit
                if (coupon.UsageCount >= coupon.MaxUsage)
                    return BadRequest(new { message = "Coupon has reached maximum usage limit." });

                // Validate coupon type
                var couponType = coupon.Type?.ToLower() ?? "product";
                
                if (couponType == "product")
                {
                    // Check if any product in order is eligible for this coupon
                    var eligibleProductIds = coupon.ProductCoupons?.Select(pc => pc.ProductId).ToList() ?? new List<int>();
                    var orderProductIds = dto.OrderItems.Select(i => i.ProductId).ToList();
                    var hasEligibleProduct = eligibleProductIds.Any() && orderProductIds.Any(id => eligibleProductIds.Contains(id));
                    
                    if (!hasEligibleProduct)
                        return BadRequest(new { message = "This coupon is not applicable to any product in your order." });
                }
                else if (couponType == "user")
                {
                    // Check if user is assigned to this coupon
                    var userCoupon = coupon.UserCoupons?.FirstOrDefault(uc => uc.UserId == userId);
                    if (userCoupon == null)
                        return BadRequest(new { message = "This coupon is not available for your account." });
                    
                    // Check per-user usage limit
                    if (userCoupon.MaxUsagePerUser.HasValue && userCoupon.UsedCount >= userCoupon.MaxUsagePerUser.Value)
                        return BadRequest(new { message = "You have reached the maximum usage limit for this coupon." });
                }
                else if (couponType == "shippingmethod")
                {
                    // Check if shipping method is eligible for this coupon
                    if (!dto.ShippingMethodId.HasValue)
                        return BadRequest(new { message = "This coupon requires a shipping method to be selected." });
                    
                    var eligibleShippingMethodIds = coupon.ShippingMethodCoupons?.Select(smc => smc.ShippingMethodId).ToList() ?? new List<int>();
                    if (!eligibleShippingMethodIds.Contains(dto.ShippingMethodId.Value))
                        return BadRequest(new { message = "This coupon is not applicable to the selected shipping method." });
                }
            }

            // Tính tổng từ server để tránh gian lận
            var productIds = dto.OrderItems.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, p => p);

            decimal computedSubtotal = 0m;
            foreach (var item in dto.OrderItems)
            {
                if (!products.TryGetValue(item.ProductId, out var prod))
                    return BadRequest(new { message = $"Product {item.ProductId} not found." });

                if (item.Quantity <= 0)
                    return BadRequest(new { message = "Quantity must be greater than 0." });

                // Kiểm tra tồn kho nếu có trường Stock
                if (prod.StockQuantity.HasValue && prod.StockQuantity.Value < item.Quantity)
                    return BadRequest(new { message = $"Insufficient stock for product {item.ProductId}." });

                computedSubtotal += item.UnitPrice * item.Quantity;
            }

            // Get shipping cost first
            decimal shippingCost = 0m;
            if (dto.ShippingMethodId.HasValue)
            {
                var shippingMethod = await _context.ShippingMethods
                    .FirstOrDefaultAsync(sm => sm.ShippingMethodId == dto.ShippingMethodId.Value && sm.IsActive);
                
                if (shippingMethod == null)
                    return BadRequest(new { message = "Invalid or inactive shipping method." });
                
                shippingCost = shippingMethod.Price;
            }

            // Áp dụng coupon
            if (coupon != null)
            {
                // Check minimum order amount
                if (coupon.MinOrderAmount.HasValue && computedSubtotal < coupon.MinOrderAmount.Value)
                    return BadRequest(new { message = $"Minimum order amount of {coupon.MinOrderAmount.Value:N0} VND is required for this coupon." });

                decimal baseAmount = computedSubtotal;
                
                // For shipping method coupons, apply to shipping cost instead
                if (coupon.Type?.ToLower() == "shippingmethod" && dto.ShippingMethodId.HasValue)
                {
                    baseAmount = shippingCost;
                }

                if (coupon.IsPercent)
                {
                    couponDiscount = Math.Round(baseAmount * coupon.DiscountValue / 100m, 0);
                    // Apply max discount limit if set
                    if (coupon.MaxDiscountAmount.HasValue && couponDiscount > coupon.MaxDiscountAmount.Value)
                        couponDiscount = coupon.MaxDiscountAmount.Value;
                }
                else
                {
                    couponDiscount = Math.Min(coupon.DiscountValue, baseAmount);
                }
            }

            var computedTotal = computedSubtotal - couponDiscount + shippingCost;
            if (computedTotal < 0) computedTotal = 0;

            
            var order = new Order
            {
                OrderNumber = dto.OrderNumber, // Use frontend-generated order number
                UserId = userId,
                AddressId = dto.AddressId,
                ShippingMethodId = dto.ShippingMethodId,
                ShippingCost = shippingCost,
                CouponId = dto.CouponId,
                TotalAmount = computedTotal,
                DiscountAmount = couponDiscount,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = "Pending",
                OrderStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
            };

            // Map OrderItems
            order.OrderItems = dto.OrderItems.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            await using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Update coupon usage
                if (coupon != null)
                {
                    coupon.UsageCount++;
                    
                    // Update user coupon usage if it's a user coupon
                    if (coupon.Type?.ToLower() == "user")
                    {
                        var userCoupon = await _context.UserCoupons
                            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CouponId == coupon.CouponId);
                        if (userCoupon != null)
                        {
                            userCoupon.UsedCount++;
                        }
                    }
                }

                // Optional: giảm tồn kho
                // foreach (var item in order.OrderItems)
                // {
                //     products[item.ProductId].Stock -= item.Quantity;
                // }
                // await _context.SaveChangesAsync();

                await trx.CommitAsync();
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
               
                return StatusCode(500, new { message = "Failed to create order", detail = ex.Message });
            }

            // gửi email cho phương thức thanh toán khác COD/bank
            await TrySendOrderEmailAsync(order);

            var isCod = order.PaymentMethod?.Equals("cod", StringComparison.OrdinalIgnoreCase) == true;
            var isBank = order.PaymentMethod?.Equals("bank", StringComparison.OrdinalIgnoreCase) == true;
            var successMessage = (isCod || isBank)
                ? "Đặt hàng thành công"
                : "Thanh toán thành công";

            return Ok(new
            {
                message = successMessage,
                orderId = order.OrderId,
                orderNumber = order.OrderNumber ?? order.OrderId.ToString(), // Return OrderNumber if available, else OrderId
                total = order.TotalAmount,
                discount = order.DiscountAmount,
                shippingCost = order.ShippingCost
            });
        }

        // Lấy danh sách đơn hàng của user
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
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
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        // Lấy tất cả đơn hàng (dùng cho dashboard)
        [HttpGet("admin/orders")]
        [Authorize(Roles = "Admin")]  // có thể thêm Roles = "Admin" nếu đã cấu hình role claim type
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)    
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
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
                    })
                })
                .ToListAsync();

            return Ok(orders);
        }

        private async Task TrySendOrderEmailAsync(Order order)
        {
            try
            {
                if (order == null) return;

                var isCod = order.PaymentMethod?.Equals("cod", StringComparison.OrdinalIgnoreCase) == true;
                var isBank = order.PaymentMethod?.Equals("bank", StringComparison.OrdinalIgnoreCase) == true;
                // Chỉ gửi email cho các phương thức khác COD/bank
                if (isCod || isBank) return;

                var user = await _context.Users.FindAsync(order.UserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

                await _context.Entry(order).Reference(o => o.Address).LoadAsync();
                await _context.Entry(order).Collection(o => o.OrderItems).LoadAsync();
                foreach (var oi in order.OrderItems)
                {
                    await _context.Entry(oi).Reference(x => x.Product).LoadAsync();
                }

                var host = Environment.GetEnvironmentVariable("SMTP_HOST");
                var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
                var userName = Environment.GetEnvironmentVariable("SMTP_USER");
                var password = Environment.GetEnvironmentVariable("SMTP_PASS");
                var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM") ?? userName;
                int port = 587;
                int.TryParse(portStr, out port);

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(userName))
                {
                    Console.WriteLine("Email not sent: SMTP config missing.");
                    return;
                }

                var subject = $"Payment successful - Order #{order.OrderId}";
                var sb = new StringBuilder();
                sb.AppendLine($"Xin chào {user.Name ?? "Bạn"},");
                sb.AppendLine("Thanh toán thành công đơn hàng của bạn.");
                sb.AppendLine($"Mã đơn: #{order.OrderId}");
                sb.AppendLine($"Phương thức: {order.PaymentMethod}");
                sb.AppendLine($"Trạng thái thanh toán: {order.PaymentStatus}");
                sb.AppendLine($"Tổng tiền: {order.TotalAmount:N0} VND");
                if (order.DiscountAmount > 0) sb.AppendLine($"Giảm giá: {order.DiscountAmount:N0} VND");
                if (order.Address != null)
                {
                    sb.AppendLine("Địa chỉ giao hàng:");
                    sb.AppendLine(
                        $"{order.Address.Street} {order.Address.Apartment} {order.Address.City} {order.Address.Province} {order.Address.Country}".Trim());
                    if (!string.IsNullOrWhiteSpace(order.Address.Phone))
                        sb.AppendLine($"SĐT: {order.Address.Phone}");
                }
                sb.AppendLine();
                sb.AppendLine("Sản phẩm:");
                foreach (var item in order.OrderItems)
                {
                    sb.AppendLine($"- {item.Product?.Name ?? ("SP#" + item.ProductId)} x{item.Quantity} · {item.UnitPrice:N0} VND");
                }
                sb.AppendLine();
                sb.AppendLine("Cảm ơn bạn đã mua hàng!");

                var message = new MailMessage(fromEmail, user.Email, subject, sb.ToString());

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(userName, password),
                    EnableSsl = true
                };
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send order email failed: {ex.Message}");
            }
        }

        public class UpdateOrderStatusDto
        {
            public string? PaymentStatus { get; set; }
            public string? OrderStatus { get; set; }
        }

        // Cập nhật trạng thái đơn hàng (admin)
        [HttpPatch("admin/orders/{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (!string.IsNullOrWhiteSpace(dto.PaymentStatus))
                order.PaymentStatus = dto.PaymentStatus;

            if (!string.IsNullOrWhiteSpace(dto.OrderStatus))
                order.OrderStatus = dto.OrderStatus;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order updated",
                orderId = order.OrderId,
                paymentStatus = order.PaymentStatus,
                orderStatus = order.OrderStatus
            });
        }

        [HttpPost("submit_coupon/{couponCode}")]
        [Authorize]
        public async Task<IActionResult> SubmitCoupon(
            [FromRoute] string couponCode,
            [FromQuery] int? shippingMethodId = null,
            [FromBody] List<int>? productIds = null)
        {
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return BadRequest(new { message = "Coupon code is required." });
            }

            // Get user ID
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            var coupon = await _context.Coupons
                .Include(c => c.ProductCoupons)
                .Include(c => c.UserCoupons)
                .Include(c => c.ShippingMethodCoupons)
                .FirstOrDefaultAsync(c => c.Code == couponCode
                                       && c.IsActive
                                       && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                                       && (c.EndDate == null || c.EndDate > DateTime.UtcNow));

            if (coupon == null)
            {
                return BadRequest(new { message = "Invalid or expired coupon code." });
            }

            // Check usage limit
            if (coupon.UsageCount >= coupon.MaxUsage)
                return BadRequest(new { message = "Coupon has reached maximum usage limit." });

            // Validate coupon type
            var couponType = coupon.Type?.ToLower() ?? "product";
            var validationErrors = new List<string>();

            if (couponType == "product")
            {
                if (productIds == null || productIds.Count == 0)
                    validationErrors.Add("Product IDs are required for product coupon.");
                else
                {
                    var eligibleProductIds = coupon.ProductCoupons?.Select(pc => pc.ProductId).ToList() ?? new List<int>();
                    var hasEligibleProduct = eligibleProductIds.Any() && productIds.Any(id => eligibleProductIds.Contains(id));
                    if (!hasEligibleProduct)
                        validationErrors.Add("This coupon is not applicable to the selected products.");
                }
            }
            else if (couponType == "user")
            {
                var userCoupon = coupon.UserCoupons?.FirstOrDefault(uc => uc.UserId == userId);
                if (userCoupon == null)
                    validationErrors.Add("This coupon is not available for your account.");
                else if (userCoupon.MaxUsagePerUser.HasValue && userCoupon.UsedCount >= userCoupon.MaxUsagePerUser.Value)
                    validationErrors.Add("You have reached the maximum usage limit for this coupon.");
            }
            else if (couponType == "shippingmethod")
            {
                if (!shippingMethodId.HasValue)
                    validationErrors.Add("Shipping method is required for shipping method coupon.");
                else
                {
                    var eligibleShippingMethodIds = coupon.ShippingMethodCoupons?.Select(smc => smc.ShippingMethodId).ToList() ?? new List<int>();
                    if (!eligibleShippingMethodIds.Contains(shippingMethodId.Value))
                        validationErrors.Add("This coupon is not applicable to the selected shipping method.");
                }
            }

            if (validationErrors.Any())
            {
                return BadRequest(new { message = string.Join(" ", validationErrors) });
            }

            return Ok(new
            {
                message = "Coupon applied successfully",
                couponId = coupon.CouponId,
                code = coupon.Code,
                type = coupon.Type,
                discountValue = coupon.DiscountValue,
                isPercent = coupon.IsPercent,
                minOrderAmount = coupon.MinOrderAmount,
                maxDiscountAmount = coupon.MaxDiscountAmount
            });
        }

        // PATCH /api/CheckOut/orders/{orderId}/mark-delivered - User marks order as delivered
        [HttpPatch("orders/{orderId}/mark-delivered")]
        public async Task<IActionResult> MarkOrderDelivered(int orderId)
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.UserId != userId)
                return StatusCode(403, new { message = "You can only update your own orders." });

            if (order.OrderStatus != "Shipped")
                return BadRequest(new { message = "Only shipped orders can be marked as delivered." });

            order.OrderStatus = "Delivered";
            order.PaymentStatus = "Completed"; // Auto-complete payment when delivered

            // Log status change
            var statusLog = new OrderStatusLog
            {
                OrderId = orderId,
                PreviousStatus = "Shipped",
                NewStatus = "Delivered",
                PreviousPaymentStatus = order.PaymentStatus,
                NewPaymentStatus = "Completed",
                ActionType = "Mark Delivered by User",
                Notes = "User confirmed delivery",
                ChangedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderStatusLogs.Add(statusLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order marked as delivered successfully.", orderId = order.OrderId });
        }

        // PATCH /api/CheckOut/orders/{orderId}/return - User requests return
        [HttpPatch("orders/{orderId}/return")]
        public async Task<IActionResult> ReturnOrder(int orderId, [FromBody] ReturnOrderDto? dto = null)
        {
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { message = "User is not authenticated." });

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { message = "Order not found." });

            if (order.UserId != userId)
                return StatusCode(403, new { message = "You can only return your own orders." });

            if (order.OrderStatus != "Shipped" && order.OrderStatus != "Delivered")
                return BadRequest(new { message = "Only shipped or delivered orders can be returned." });

            order.OrderStatus = "Returned";
            order.PaymentStatus = "Refunded"; // Auto-refund when returned

            // Log status change
            var statusLog = new OrderStatusLog
            {
                OrderId = orderId,
                PreviousStatus = order.OrderStatus,
                NewStatus = "Returned",
                PreviousPaymentStatus = order.PaymentStatus,
                NewPaymentStatus = "Refunded",
                ActionType = "Return Request by User",
                Notes = dto?.Reason ?? "User requested return",
                ChangedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.OrderStatusLogs.Add(statusLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Return request submitted successfully.", orderId = order.OrderId });
        }

        public class ReturnOrderDto
        {
            public string? Reason { get; set; }
        }
    }
}