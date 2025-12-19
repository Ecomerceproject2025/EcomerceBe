using System.ComponentModel.DataAnnotations;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(Roles = "Admin")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private readonly IConfiguration _configuration;

        public OrderController(AppDbContext context, ILogger<OrderController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private string GetDeliverySecret()
        {
            var secret = Environment.GetEnvironmentVariable("DELIVERY_TOKEN_SECRET");
            if (string.IsNullOrWhiteSpace(secret))
            {
                // fallback to appsettings Delivery:DeliveryTokenSecret
                secret = _configuration["Delivery:DeliveryTokenSecret"];
            }
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("DELIVERY_TOKEN_SECRET is not configured.");
            return secret;
        }

        // GET /api/orders/customer-locations?top=6
        // Thống kê vị trí khách hàng theo quốc gia dựa trên địa chỉ trong đơn hàng
        [HttpGet("customer-locations")]
        public async Task<IActionResult> GetCustomerLocations([FromQuery] int top = 6)
        {
            if (top < 1) top = 6;
            // Lấy các đơn hàng có địa chỉ gắn kèm
            var query = _context.Orders
                .Include(o => o.Address)
                .AsNoTracking()
                .Where(o => o.Address != null);

            var totalCustomers = await query
                .Select(o => new { o.UserId, Country = o.Address.Country })
                .Distinct()
                .CountAsync();

            if (totalCustomers == 0)
            {
                return Ok(new
                {
                    data = Array.Empty<object>(),
                    total = 0
                });
            }

            // Nhóm theo country
            var grouped = await query
                .Select(o => new { o.UserId, Country = o.Address.Country })
                .Distinct() // mỗi user-country 1 lần
                .GroupBy(x => x.Country)
                .Select(g => new
                {
                    Country = g.Key,
                    CustomerCount = g.Count()
                })
                .OrderByDescending(x => x.CustomerCount)
                .ToListAsync();

            // Map country -> lat/lng & code (demo mapping đơn giản)
            var countryMap = new Dictionary<string, (string Code, double Lat, double Lng)>(StringComparer.OrdinalIgnoreCase)
            {
                { "usa", ("US", 37.2580397, -104.657039) },
                { "united states", ("US", 37.2580397, -104.657039) },
                { "us", ("US", 37.2580397, -104.657039) },
                { "france", ("FR", 46.2276, 2.2137) },
                { "pháp", ("FR", 46.2276, 2.2137) },
                { "vietnam", ("VN", 14.0583, 108.2772) },
                { "việt nam", ("VN", 14.0583, 108.2772) },
                { "india", ("IN", 20.7504374, 73.7276105) },
                { "australia", ("AU", -25.2744, 133.7751) },
            };

            var results = grouped.Take(top).Select(g =>
            {
                var key = g.Country?.Trim() ?? "";
                var lookupKey = key.ToLowerInvariant();

                countryMap.TryGetValue(lookupKey, out var info);

                var percentage = Math.Round((double)g.CustomerCount / totalCustomers * 100, 1);

                return new
                {
                    countryCode = string.IsNullOrEmpty(info.Code) ? lookupKey.ToUpperInvariant() : info.Code,
                    countryName = string.IsNullOrWhiteSpace(key) ? "Unknown" : key,
                    customerCount = g.CustomerCount,
                    percentage,
                    latitude = info.Lat,
                    longitude = info.Lng
                };
            }).ToList();

            return Ok(new
            {
                data = results,
                total = totalCustomers
            });
        }

        // GET /api/orders/customer-regions?level=province|city&top=6
        // Thống kê chi tiết theo Tỉnh/Thành phố hoặc Thành phố (city)
        [HttpGet("customer-regions")]
        public async Task<IActionResult> GetCustomerRegions([FromQuery] string level = "province", [FromQuery] int top = 6)
        {
            level = (level ?? "province").Trim().ToLowerInvariant();
            var isCity = level == "city";
            if (top < 1) top = 6;

            var query = _context.Orders
                .Include(o => o.Address)
                .AsNoTracking()
                .Where(o => o.Address != null);

            var totalCustomers = await query
                .Select(o => new
                {
                    o.UserId,
                    Region = isCity ? o.Address.City : o.Address.Province
                })
                .Distinct()
                .CountAsync();

            if (totalCustomers == 0)
            {
                return Ok(new
                {
                    data = Array.Empty<object>(),
                    total = 0
                });
            }

            var grouped = await query
                .Select(o => new
                {
                    o.UserId,
                    Region = isCity ? o.Address.City : o.Address.Province
                })
                .Distinct()
                .GroupBy(x => x.Region)
                .Select(g => new
                {
                    Region = g.Key,
                    CustomerCount = g.Count()
                })
                .OrderByDescending(x => x.CustomerCount)
                .ToListAsync();

            var results = grouped.Take(top).Select(g =>
            {
                var name = g.Region?.Trim();
                var percentage = Math.Round((double)g.CustomerCount / totalCustomers * 100, 1);

                return new
                {
                    name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
                    customerCount = g.CustomerCount,
                    percentage
                };
            }).ToList();

            return Ok(new
            {
                data = results,
                total = totalCustomers,
                level = isCity ? "city" : "province"
            });
        }

        private string ComputeDeliveryToken(int orderId, DateTime createdAtUtc)
        {
            var secret = GetDeliverySecret();
            var payload = $"{orderId}:{createdAtUtc.Ticks}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var sig = Convert.ToHexString(hash).ToLowerInvariant();
            return $"{payload}:{sig}";
        }

        private bool ValidateDeliveryToken(string token, out int orderId)
        {
            orderId = 0;
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Split(':');
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out orderId)) return false;
            if (!long.TryParse(parts[1], out var ticks)) return false;
            var expected = ComputeDeliveryToken(orderId, new DateTime(ticks, DateTimeKind.Utc));
            return string.Equals(expected, token, StringComparison.Ordinal);
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
                    ProductName = oi.Product.Name,
                    size = oi.Size,
                    color = oi.Color
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

        [HttpGet("{id}/delivery-qr")]
        public async Task<IActionResult> GetDeliveryQr(int id, [FromQuery] string? confirmBaseUrl = null)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound(new { message = "Order not found." });

            var token = ComputeDeliveryToken(order.OrderId, order.CreatedAt);
            var baseUrl = confirmBaseUrl ?? Environment.GetEnvironmentVariable("DELIVERY_CONFIRM_BASEURL") ?? "";
            var confirmUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? $"https://example.com/ship/confirm?token={token}"
                : $"{baseUrl.TrimEnd('/')}/ship/confirm?token={Uri.EscapeDataString(token)}";

            return Ok(new
            {
                token,
                confirmUrl
            });
        }

        [HttpPost("confirm-delivery")]
        [Authorize(Roles = "Shipper,Admin")]
        public async Task<IActionResult> ConfirmDelivery([FromBody] ConfirmDeliveryDto dto)
        {
            // Get authenticated shipper user
            var userIdClaim = User.FindFirst("id")
                            ?? User.FindFirst("Id")
                            ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var shipperUserId))
                return Unauthorized(new { message = "Shipper authentication required." });

            // Verify user has Shipper role
            var user = await _context.Users.FindAsync(shipperUserId);
            if (user == null)
                return Unauthorized(new { message = "Shipper account not found." });

            if (!user.Role.Equals("Shipper", StringComparison.OrdinalIgnoreCase) && 
                !user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return Forbid("Only shippers can confirm delivery.");

            if (dto == null || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest(new { message = "Token is required." });

            if (!ValidateDeliveryToken(dto.Token, out var orderId))
                return BadRequest(new { message = "Invalid token." });

            // Load order with all necessary navigation properties for inventory deduction
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductSizes)
                            .ThenInclude(ps => ps.Size) // Include Size for proper matching
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductSizes)
                            .ThenInclude(ps => ps.ProductColors)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.FlashSaleItems)
                            .ThenInclude(fsi => fsi.FlashSale)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound(new { message = "Order not found." });

            // Idempotency check: prevent double deduction
            if (order.OrderStatus == "Delivered")
                return Ok(new { message = "Order already delivered.", orderId = order.OrderId });

            // Validate order status transition
            if (order.OrderStatus != "Shipped")
                return BadRequest(new { message = "Only shipped orders can be confirmed as delivered." });

            // Wrap all operations in a transaction for atomicity
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate inventory availability before deduction
                foreach (var item in order.OrderItems)
                {
                    if (item.Product == null) continue;

                    // Compute total available stock (use StockQuantity if set, else sum variant quantities)
                    var totalVariantQty = item.Product.ProductSizes.SelectMany(ps => ps.ProductColors).Sum(pc => pc.Quantity);
                    var currentTotalQty = item.Product.StockQuantity ?? totalVariantQty;
                    if (currentTotalQty < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new 
                        { 
                            message = $"Insufficient inventory for product {item.Product.Name} (ID: {item.ProductId}). Available: {currentTotalQty}, Required: {item.Quantity}" 
                        });
                    }

                    // Validate ProductColor.Quantity if size/color specified
                    // Validate variant stock if applicable
                    var sizeKey = item.Size?.Trim().ToLowerInvariant();
                    var colorKey = item.Color?.Trim().ToLowerInvariant();

                    // Resolve variant strictly by ProductVariantId or size/color
                    ProductColor? colorEntry = null;
                    ProductSize? sizeEntry = null;
                    if (item.ProductVariantId.HasValue)
                    {
                        // 1) ProductVariantId as ProductColorId
                        colorEntry = item.Product.ProductSizes.SelectMany(ps => ps.ProductColors)
                            .FirstOrDefault(pc => pc.ProductColorId == item.ProductVariantId.Value);

                        // 2) ProductVariantId as ProductSizeId (needs color)
                        if (colorEntry == null)
                        {
                            sizeEntry = item.Product.ProductSizes.FirstOrDefault(ps => ps.ProductSizeId == item.ProductVariantId.Value);
                            if (sizeEntry != null)
                            {
                                if (!string.IsNullOrEmpty(colorKey))
                                {
                                    colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                        pc.ColorCode.Trim().ToLowerInvariant() == colorKey);
                                }
                                else if (sizeEntry.ProductColors.Count == 1)
                                {
                                    colorEntry = sizeEntry.ProductColors.First();
                                }
                                else
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new
                                    {
                                        message = "Color is required for this size variant."
                                    });
                                }
                            }
                        }
                    }

                    // Fallback to size/color text matching
                    if (colorEntry == null && (!string.IsNullOrEmpty(sizeKey) || !string.IsNullOrEmpty(colorKey)))
                    {
                        sizeEntry ??= item.Product.ProductSizes.FirstOrDefault(ps =>
                            (!string.IsNullOrEmpty(ps.CustomValue) && ps.CustomValue.Trim().ToLowerInvariant() == sizeKey) ||
                            (ps.Size != null && ps.Size.Name.Trim().ToLowerInvariant() == sizeKey));

                        if (sizeEntry != null)
                        {
                            if (!string.IsNullOrEmpty(colorKey))
                            {
                                colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                    pc.ColorCode.Trim().ToLowerInvariant() == colorKey);
                            }
                            else if (sizeEntry.ProductColors.Count == 1)
                            {
                                colorEntry = sizeEntry.ProductColors.First();
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new
                                {
                                    message = "Color is required for this size."
                                });
                            }
                        }
                    }

                    if (colorEntry == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = "Cannot resolve product variant (size/color)."
                        });
                    }

                    if (colorEntry.Quantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new 
                        { 
                            message = $"Insufficient inventory for product {item.Product.Name} - Variant out of stock. Available: {colorEntry.Quantity}, Required: {item.Quantity}" 
                        });
                    }
                }

                // All validations passed, proceed with inventory deduction
                var prevPayment = order.PaymentStatus;
                order.OrderStatus = "Delivered";
                order.PaymentStatus = "Completed";

                // Deduct inventory for each order item
                foreach (var item in order.OrderItems)
                {
                    if (item.Product == null) continue;

                    // Deduct from Product.StockQuantity (TotalQuantity) using computed total
                    var totalVariantQty = item.Product.ProductSizes.SelectMany(ps => ps.ProductColors).Sum(pc => pc.Quantity);
                    var currentTotalQty = item.Product.StockQuantity ?? totalVariantQty;
                    item.Product.StockQuantity = Math.Max(0, currentTotalQty - item.Quantity);

                    // Update sold count
                    item.Product.SoldCount = (item.Product.SoldCount ?? 0) + item.Quantity;

                    // Deduct from ProductColor.Quantity and ensure ProductSize consistency
                    var sizeKey = item.Size?.Trim().ToLowerInvariant();
                    var colorKey = item.Color?.Trim().ToLowerInvariant();

                    // Resolve variant strictly by ProductVariantId or size/color
                    ProductColor? colorEntry = null;
                    ProductSize? sizeEntry = null;
                    if (item.ProductVariantId.HasValue)
                    {
                        colorEntry = item.Product.ProductSizes.SelectMany(ps => ps.ProductColors)
                            .FirstOrDefault(pc => pc.ProductColorId == item.ProductVariantId.Value);

                        if (colorEntry == null)
                        {
                            sizeEntry = item.Product.ProductSizes.FirstOrDefault(ps => ps.ProductSizeId == item.ProductVariantId.Value);
                            if (sizeEntry != null)
                            {
                                if (!string.IsNullOrEmpty(colorKey))
                                {
                                    colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                        pc.ColorCode.Trim().ToLowerInvariant() == colorKey);
                                }
                                else if (sizeEntry.ProductColors.Count == 1)
                                {
                                    colorEntry = sizeEntry.ProductColors.First();
                                }
                            }
                        }
                    }

                    if (colorEntry == null && (!string.IsNullOrEmpty(sizeKey) || !string.IsNullOrEmpty(colorKey)))
                    {
                        sizeEntry ??= item.Product.ProductSizes.FirstOrDefault(ps =>
                            (!string.IsNullOrEmpty(ps.CustomValue) && ps.CustomValue.Trim().ToLowerInvariant() == sizeKey) ||
                            (ps.Size != null && ps.Size.Name.Trim().ToLowerInvariant() == sizeKey));

                        if (sizeEntry != null)
                        {
                            if (!string.IsNullOrEmpty(colorKey))
                            {
                                colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                    pc.ColorCode.Trim().ToLowerInvariant() == colorKey);
                            }
                            else if (sizeEntry.ProductColors.Count == 1)
                            {
                                colorEntry = sizeEntry.ProductColors.First();
                            }
                        }
                    }

                    if (colorEntry == null)
                    {
                        continue; // Should not happen due to validation above, but guard
                    }

                    if (colorEntry != null)
                    {
                        // Deduct from ProductColor.Quantity
                        colorEntry.Quantity = Math.Max(0, colorEntry.Quantity - item.Quantity);

                        // Also deduct saleQuantity per color if defined (flash sale per color)
                        if (colorEntry.SaleQuantity.HasValue && colorEntry.SaleQuantity.Value > 0)
                        {
                            var newSaleQty = colorEntry.SaleQuantity.Value - item.Quantity;
                            colorEntry.SaleQuantity = newSaleQty < 0 ? 0 : newSaleQty;
                        }

                        // Update FlashSale sold count if product is in an active flash sale
                        var now = DateTime.UtcNow;
                        var flashSaleItem = item.Product.FlashSaleItems?
                            .FirstOrDefault(fsi => fsi.ProductId == item.ProductId
                                                   && fsi.FlashSale != null
                                                   && fsi.FlashSale.StartTime <= now
                                                   && fsi.FlashSale.EndTime >= now);
                        if (flashSaleItem != null)
                        {
                            flashSaleItem.Sold += item.Quantity;
                            if (flashSaleItem.Sold < 0) flashSaleItem.Sold = 0;
                            // Optional: cap Sold to saleQuantity
                            if (flashSaleItem.Sold > flashSaleItem.saleQuantity)
                                flashSaleItem.Sold = flashSaleItem.saleQuantity;
                        }
                    }
                }

                // Log status change
                _context.OrderStatusLogs.Add(new OrderStatusLog
                {
                    OrderId = orderId,
                    PreviousStatus = "Shipped",
                    NewStatus = "Delivered",
                    PreviousPaymentStatus = prevPayment,
                    NewPaymentStatus = "Completed",
                    ActionType = "Confirm Delivery (Shipper)",
                    Notes = $"Confirmed via QR token by shipper: {user.Name} ({user.Email})",
                    ChangedByUserId = shipperUserId,
                    CreatedAt = DateTime.UtcNow
                });

                // Save all changes atomically
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to confirm delivery for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "Failed to confirm delivery. Please try again.", detail = ex.Message });
            }

            return Ok(new { message = "Delivery confirmed.", orderId = order.OrderId });
        }

        public class ConfirmDeliveryDto
        {
            public string Token { get; set; } = string.Empty;
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
                    size = oi.Size,
                    color = oi.Color,
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
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductSizes)
                            .ThenInclude(ps => ps.Size)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductSizes)
                            .ThenInclude(ps => ps.ProductColors)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.FlashSaleItems)
                            .ThenInclude(fsi => fsi.FlashSale)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound(new { message = "Order not found." });

            var isDeliveredTarget = !string.IsNullOrWhiteSpace(dto.OrderStatus) &&
                                    dto.OrderStatus.Trim().Equals("Delivered", StringComparison.OrdinalIgnoreCase);
            if (isDeliveredTarget)
            {
                // Idempotent: already delivered
                if (order.OrderStatus == "Delivered")
                {
                    return Ok(new { message = "Order already delivered.", orderId = order.OrderId });
                }

                // Only allow transition from Shipped -> Delivered
                if (order.OrderStatus != "Shipped")
                {
                    return BadRequest(new
                    {
                        message = $"Only shipped orders can be marked as delivered. Current status: {order.OrderStatus}"
                    });
                }

                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate inventory
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product == null) continue;

                        var currentTotalQty = item.Product.StockQuantity ?? 0;
                        if (currentTotalQty < item.Quantity)
                        {
                            await tx.RollbackAsync();
                            return BadRequest(new
                            {
                                message = $"Insufficient inventory for product {item.Product.Name} (ID: {item.ProductId}). Available: {currentTotalQty}, Required: {item.Quantity}"
                            });
                        }

                        var sizeKey = item.Size?.Trim().ToLowerInvariant();
                        var colorKey = item.Color?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(sizeKey) && !string.IsNullOrEmpty(colorKey))
                        {
                            var sizeEntry = item.Product.ProductSizes.FirstOrDefault(ps =>
                                (!string.IsNullOrEmpty(ps.CustomValue) && ps.CustomValue.Trim().ToLowerInvariant() == sizeKey) ||
                                (ps.Size != null && ps.Size.Name.Trim().ToLowerInvariant() == sizeKey));

                            if (sizeEntry != null)
                            {
                                var colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                    pc.ColorCode.Trim().ToLowerInvariant() == colorKey);

                                if (colorEntry != null && colorEntry.Quantity < item.Quantity)
                                {
                                    await tx.RollbackAsync();
                                    return BadRequest(new
                                    {
                                        message = $"Insufficient inventory for product {item.Product.Name} - Size: {item.Size}, Color: {item.Color}. Available: {colorEntry.Quantity}, Required: {item.Quantity}"
                                    });
                                }
                            }
                        }
                    }

                    var deliveredPreviousOrderStatus = order.OrderStatus;
                    var deliveredPreviousPaymentStatus = order.PaymentStatus;

                    // Apply status/payment
                    order.OrderStatus = "Delivered";
                    order.PaymentStatus = string.IsNullOrWhiteSpace(dto.PaymentStatus)
                        ? "Completed"
                        : dto.PaymentStatus.Trim();

                    // Deduct inventory
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product == null) continue;

                        var currentTotalQty = item.Product.StockQuantity ?? 0;
                        item.Product.StockQuantity = Math.Max(0, currentTotalQty - item.Quantity);

                        item.Product.SoldCount = (item.Product.SoldCount ?? 0) + item.Quantity;

                        var sizeKey = item.Size?.Trim().ToLowerInvariant();
                        var colorKey = item.Color?.Trim().ToLowerInvariant();
                        if (!string.IsNullOrEmpty(sizeKey) && !string.IsNullOrEmpty(colorKey))
                        {
                            var sizeEntry = item.Product.ProductSizes.FirstOrDefault(ps =>
                                (!string.IsNullOrEmpty(ps.CustomValue) && ps.CustomValue.Trim().ToLowerInvariant() == sizeKey) ||
                                (ps.Size != null && ps.Size.Name.Trim().ToLowerInvariant() == sizeKey));

                            if (sizeEntry != null)
                            {
                                var colorEntry = sizeEntry.ProductColors.FirstOrDefault(pc =>
                                    pc.ColorCode.Trim().ToLowerInvariant() == colorKey);

                                if (colorEntry != null)
                                {
                                    colorEntry.Quantity -= item.Quantity;
                                    if (colorEntry.Quantity < 0) colorEntry.Quantity = 0;
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    // Log change
                    await LogStatusChangeAsync(
                        order.OrderId,
                        deliveredPreviousOrderStatus,
                        order.OrderStatus,
                        deliveredPreviousPaymentStatus,
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
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Failed to mark order delivered for OrderId: {OrderId}", order.OrderId);
                    return StatusCode(500, new { message = "Failed to update order status.", detail = ex.Message });
                }
            }

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

