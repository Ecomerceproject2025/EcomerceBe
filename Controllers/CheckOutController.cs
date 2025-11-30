using EcomerceBE.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CheckOutController :ControllerBase
    {
        private readonly AppDbContext _context;
        public CheckOutController(AppDbContext context)
        {
            _context = context;
        }




        [HttpPost("submit_coupon/{couponCode}")] // Sửa lại route cho chuẩn convention
        public async Task<IActionResult> SubmitCoupon([FromRoute] string couponCode)
        {
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(couponCode))
            {
                return BadRequest(new { message = "Coupon code is required." });
            }

            // 2. Truy vấn Async
            // Kiểm tra: Đúng mã, Đang hoạt động, Chưa hết hạn, Đã bắt đầu (nếu có ngày bắt đầu)
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode
                                       && c.IsActive
                                       && (c.StartDate == null || c.StartDate <= DateTime.UtcNow)
                                       && (c.EndDate == null || c.EndDate > DateTime.UtcNow));

            // 3. Xử lý kết quả
            if (coupon == null)
            {
                return BadRequest(new { message = "Invalid or expired coupon code." });
            }

            // (Optional) Kiểm tra số lượng giới hạn nếu có
            // if (coupon.Quantity <= 0) return BadRequest(new { message = "Coupon is out of stock." });

            // 4. Trả về thông tin coupon cho FE tính toán
            return Ok(new
            {
                message = "Coupon applied successfully",
                code = coupon.Code,

                discountValue = coupon.DiscountValue,
                coupon.IsPercent

            });
        }


    }


}
