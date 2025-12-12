using EcomerceBE.Models; // Thay bằng namespace chứa DTO của bạn
using EcomerceBE.Service.flashSale;
using Microsoft.AspNetCore.Mvc;

namespace EcomerceBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlashSaleController : ControllerBase
    {
        private readonly IFlashSaleService _flashSaleService;

        public FlashSaleController(IFlashSaleService flashSaleService)
        {
            _flashSaleService = flashSaleService;
        }

        // 1. API Lấy tất cả Flash Sale
        // GET: api/flashsale
        [HttpGet("GetAllFlashSales")]
        public async Task<IActionResult> GetAllFlashSales()
        {
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
    }
}
