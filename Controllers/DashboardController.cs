using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // API: Get monthly sales data
        [HttpGet("monthly-sales")]
        public async Task<IActionResult> GetMonthlySales([FromQuery] int year = 0)
        {
            if (year == 0) year = DateTime.UtcNow.Year;

            var monthlySales = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt.Year == year && o.OrderStatus != "Canceled")
                .GroupBy(o => o.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Sales = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Fill missing months with 0
            var result = new List<object>();
            for (int i = 1; i <= 12; i++)
            {
                var monthData = monthlySales.FirstOrDefault(m => m.Month == i);
                result.Add(new
                {
                    Month = i,
                    Sales = monthData != null ? (decimal)monthData.Sales : 0
                });
            }

            return Ok(result);
        }

        // API: Get statistics (Sales and Revenue) by period
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] string period = "monthly")
        {
            var now = DateTime.UtcNow;
            IQueryable<Order> query = _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus != "Canceled");

            List<object> result = new List<object>();

            if (period == "monthly")
            {
                query = query.Where(o => o.CreatedAt.Year == now.Year);
                var data = await query
                    .GroupBy(o => o.CreatedAt.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Sales = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                for (int i = 1; i <= 12; i++)
                {
                    var monthData = data.FirstOrDefault(m => m.Month == i);
                    result.Add(new
                    {
                        Month = i,
                        Sales = monthData != null ? monthData.Sales : 0,
                        Revenue = monthData != null ? (decimal)monthData.Revenue : 0
                    });
                }
            }
            else if (period == "quarterly")
            {
                query = query.Where(o => o.CreatedAt.Year == now.Year);
                var data = await query
                    .GroupBy(o => (o.CreatedAt.Month - 1) / 3 + 1)
                    .Select(g => new
                    {
                        Quarter = g.Key,
                        Sales = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Quarter)
                    .ToListAsync();

                for (int i = 1; i <= 4; i++)
                {
                    var quarterData = data.FirstOrDefault(q => q.Quarter == i);
                    result.Add(new
                    {
                        Quarter = i,
                        Sales = quarterData != null ? quarterData.Sales : 0,
                        Revenue = quarterData != null ? (decimal)quarterData.Revenue : 0
                    });
                }
            }
            else if (period == "annually")
            {
                var startYear = now.Year - 4; // Last 5 years
                query = query.Where(o => o.CreatedAt.Year >= startYear);
                var data = await query
                    .GroupBy(o => o.CreatedAt.Year)
                    .Select(g => new
                    {
                        Year = g.Key,
                        Sales = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Year)
                    .ToListAsync();

                for (int i = startYear; i <= now.Year; i++)
                {
                    var yearData = data.FirstOrDefault(y => y.Year == i);
                    result.Add(new
                    {
                        Year = i,
                        Sales = yearData != null ? yearData.Sales : 0,
                        Revenue = yearData != null ? (decimal)yearData.Revenue : 0
                    });
                }
            }

            return Ok(result);
        }

        // API: Get products by category
        [HttpGet("products-by-category")]
        public async Task<IActionResult> GetProductsByCategory()
        {
            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Category)
                .GroupBy(p => p.Category != null ? p.Category.Name : "Uncategorized")
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToListAsync();

            return Ok(data);
        }

        // API: Get top selling products with full product details
        // AllowAnonymous: This endpoint is used on public home page
        [AllowAnonymous]
        [HttpGet("top-selling-products")]
        public async Task<IActionResult> GetTopSellingProducts([FromQuery] int limit = 8)
        {
            var topProducts = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Images)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Reviews)
                .Where(oi => oi.Order.OrderStatus != "Canceled")
                .GroupBy(oi => new { 
                    oi.Product.ProductId, 
                    oi.Product.Name,
                    oi.Product.Price,
                    oi.Product.ImportPrice,
                    oi.Product.StarRating,
                    oi.Product.productType
                })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Price = g.Key.Price,
                    ImportPrice = g.Key.ImportPrice,
                    StarRating = g.Key.StarRating,
                    ProductType = g.Key.productType,
                    FirstImage = g.First().Product.Images.OrderBy(img => img.ProductImageId).Select(img => img.ImageUrl).FirstOrDefault() ?? "",
                    ReviewCount = g.First().Product.Reviews.Count
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(limit)
                .ToListAsync();

            // Get flash sale prices for products that are flash sale
            var productIds = topProducts.Select(p => p.ProductId).ToList();
            var flashSaleItems = await _context.FlashSaleItems
                .AsNoTracking()
                .Where(fsi => productIds.Contains(fsi.ProductId))
                .Select(fsi => new
                {
                    ProductId = fsi.ProductId,
                    DiscountPrice = fsi.DiscountPrice,
                    SaleQuantity = fsi.saleQuantity
                })
                .ToListAsync();

            var flashSaleDict = flashSaleItems.ToDictionary(fsi => fsi.ProductId);

            var result = topProducts.Select(p => new
            {
                id = p.ProductId,
                name = p.ProductName,
                image = !string.IsNullOrEmpty(p.FirstImage) ? new[] { p.FirstImage } : new string[0],
                price = p.Price,
                salePrice = flashSaleDict.ContainsKey(p.ProductId) 
                    ? flashSaleDict[p.ProductId].DiscountPrice 
                    : (decimal?)null,
                discount = flashSaleDict.ContainsKey(p.ProductId) && p.Price > 0
                    ? (int)Math.Round(((p.Price - flashSaleDict[p.ProductId].DiscountPrice) / p.Price) * 100)
                    : (int?)null,
                starRatingrating = p.StarRating,
                reviewCounts = p.ReviewCount,
                unitsSold = p.UnitsSold
            }).ToList();

            return Ok(result);
        }

        // API: Get ecommerce metrics (Customers, Orders)
        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);

            // Total customers
            var totalCustomers = await _context.Users
                .AsNoTracking()
                .CountAsync();

            var lastMonthCustomers = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= lastMonth && u.CreatedAt < now)
                .CountAsync();

            var previousMonthCustomers = await _context.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= lastMonth.AddMonths(-1) && u.CreatedAt < lastMonth)
                .CountAsync();

            var customerChangePercent = previousMonthCustomers > 0
                ? ((lastMonthCustomers - previousMonthCustomers) / (double)previousMonthCustomers) * 100
                : (lastMonthCustomers > 0 ? 100 : 0);

            // Total orders
            var totalOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderStatus != "Canceled")
                .CountAsync();

            var lastMonthOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= lastMonth && o.CreatedAt < now && o.OrderStatus != "Canceled")
                .CountAsync();

            var previousMonthOrders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= lastMonth.AddMonths(-1) && o.CreatedAt < lastMonth && o.OrderStatus != "Canceled")
                .CountAsync();

            var orderChangePercent = previousMonthOrders > 0
                ? ((lastMonthOrders - previousMonthOrders) / (double)previousMonthOrders) * 100
                : (lastMonthOrders > 0 ? 100 : 0);

            return Ok(new
            {
                Customers = new
                {
                    Total = totalCustomers,
                    ChangePercent = Math.Round(customerChangePercent, 2),
                    IsPositive = customerChangePercent >= 0
                },
                Orders = new
                {
                    Total = totalOrders,
                    ChangePercent = Math.Round(orderChangePercent, 2),
                    IsPositive = orderChangePercent >= 0
                }
            });
        }

        // API: Get recent orders
        [HttpGet("recent-orders")]
        public async Task<IActionResult> GetRecentOrders([FromQuery] int limit = 5)
        {
            var data = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    ProductName = o.OrderItems.FirstOrDefault() != null && o.OrderItems.FirstOrDefault()!.Product != null
                        ? o.OrderItems.FirstOrDefault()!.Product.Name
                        : "Unknown",
                    ProductImage = o.OrderItems.FirstOrDefault() != null && o.OrderItems.FirstOrDefault()!.Product != null && o.OrderItems.FirstOrDefault()!.Product.Images.Any()
                        ? o.OrderItems.FirstOrDefault()!.Product.Images.First().ImageUrl
                        : "",
                    Variants = o.OrderItems.Count,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}

