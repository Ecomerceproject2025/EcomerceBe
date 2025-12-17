using EcomerceBE.Service.flashSale;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcomerceBE.Data;
using static EcomerceBE.Controllers.ProductViewController;

namespace EcomerceBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlashSaleController : ControllerBase
    {
        private readonly IFlashSaleService _flashSaleService;
        private readonly AppDbContext _context;

        public FlashSaleController(IFlashSaleService flashSaleService, AppDbContext context)
        {
            _flashSaleService = flashSaleService;
            _context = context;
        }

        // 1. API Lấy tất cả Flash Sale
        // GET: api/flashsale
        [HttpGet("GetAllFlashSales")]
        public async Task<IActionResult> GetAllFlashSales()
        {
            await _flashSaleService.CleanupExpiredFlashSalesAsync();
            var result = await _flashSaleService.GetAllFlashSalesWithItemsAsync();
            return Ok(result);
        }

        // 2. API Tạo mới Flash Sale
        // POST: api/flashsale
     

        // 3. API Cập nhật Flash Sale
        [HttpPut("SetTimeFlashSale")]
        public async Task<IActionResult> CreateUpdateFlashSale(int ?id, [FromBody] FlashSaleCreateDTO dto)
        {
            try
            {
                // Truyền id vào để Service hiểu là cập nhật
                var result = await _flashSaleService.CreateOrUpdateFlashSaleAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Bắt lỗi "Flash sale with id not found" hoặc lỗi ngày tháng
                return BadRequest(new { message = ex.Message });
            }
        }

        // 4. API Xóa sản phẩm khỏi Flash Sale (Mặc định FlashSaleId = 1 như trong service)
        // DELETE
        [HttpDelete("{productId}/remove-item")]
        public async Task<IActionResult> RemoveProductFromSale(int productId)
        {
            // Lưu ý: productId ở đây ứng với tham số flashSaleItemId (là ProductId) trong service của bạn
            var isDeleted = await _flashSaleService.RemoveSaleProductAsync(productId);

            if (!isDeleted)
            {
                return NotFound(new { message = "Do not find item Flash Sale #1" });
            }

            return Ok(new { message = "Item was remove" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFlashSale(int id)
        {
            var deleted = await _flashSaleService.DeleteFlashSaleAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = "Flash sale not found" });
            }

            return Ok(new { message = "Flash sale deleted" });
        }

        // API Lấy FlashSaleItem theo ProductId với đầy đủ thông tin Product
        [HttpGet("GetItemByProductId/{productId:int}")]
        public async Task<IActionResult> GetFlashSaleItemByProductId(int productId)
        {
            await _flashSaleService.CleanupExpiredFlashSalesAsync();
            var flashSaleItem = await _context.FlashSaleItems
                .AsNoTracking()
                .Include(fsi => fsi.Product)
                    .ThenInclude(p => p.Images)
                .Include(fsi => fsi.Product)
                    .ThenInclude(p => p.ProductSizes)
                        .ThenInclude(ps => ps.ProductColors)
                .Include(fsi => fsi.Product)
                    .ThenInclude(p => p.ProductSizes)
                        .ThenInclude(ps => ps.Size)
                .Include(fsi => fsi.Product)
                    .ThenInclude(p => p.Reviews) // Include Reviews để đếm
                .Where(fsi => fsi.ProductId == productId)
                .Select(fsi => new ProductDetailDto
                {
                    Id = fsi.Product.ProductId,
                    Name = fsi.Product.Name,
                    image = fsi.Product.Images.Select(i => i.ImageUrl).ToList(),
                    Price = fsi.DiscountPrice, // Giá sale từ FlashSaleItem
                    Description = fsi.Product.Description,
                    StarRatingrating = fsi.Product.StarRating,
                    ReviewCounts = fsi.Product.Reviews != null ? fsi.Product.Reviews.Count : 0, // Đếm số reviews
                    ReturnDeliveryDay = fsi.Product.ReturnDeliveryDay,
                    isFlashSale = true,
                    flashSaleRemaining = fsi.saleQuantity - fsi.Sold, // Số lượng flash sale còn lại
                    Sizes = fsi.Product.ProductSizes
                        .Select(ps => new SizeWithColorsDto
                        {
                            SizeName = ps.Size != null ? ps.Size.Name : (ps.CustomValue ?? string.Empty),
                            Color = ps.ProductColors != null
                                ? ps.ProductColors
                                    .Select(pc => new ColorAndquantity
                                    {
                                        ColorCode = pc.ColorCode,
                                        quantity = pc.Quantity,
                                        saleQuantity = pc.SaleQuantity // Sale quantity cho từng màu
                                    })
                                    .ToList()
                                : new List<ColorAndquantity>(),
                        })
                        .ToList(),
                    totalquantity = fsi.Product.ProductSizes
                        .SelectMany(ps => ps.ProductColors)
                        .Sum(pc => pc.Quantity),
                })
                .FirstOrDefaultAsync();

            if (flashSaleItem == null)
            {
                return NotFound(new { message = "Flash sale item not found" });
            }

            return Ok(flashSaleItem);
        }
    }
}
