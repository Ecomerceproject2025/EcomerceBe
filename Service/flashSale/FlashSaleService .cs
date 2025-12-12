    using EcomerceBE.Data;
    using EcomerceBE.Models;
    using Microsoft.EntityFrameworkCore;

    namespace EcomerceBE.Service.flashSale
    {
        public class FlashSaleService : IFlashSaleService
        {
            private readonly AppDbContext _db;

            public FlashSaleService(AppDbContext db)
            {
                _db = db;
            }






        // Models/DTOs/FlashSaleDto.cs
        public class FlashSaleDto
        {
            public int FlashSaleId { get; set; }
            public string Name { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public List<FlashSaleItemDto> FlashSaleItems { get; set; } = new();
        }

        public class FlashSaleItemDto
        {
            public int FlashSaleItemId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal DiscountPrice { get; set; }
            public decimal OriginalPrice { get; set; }
            public string ImageUrl { get; set; }
            public int saleQuantity { get; set; }
            public int Sold { get; set; }
        }

        // Repository
        public async Task<List<FlashSaleDto>> GetAllFlashSalesWithItemsAsync()
        {
            var now = DateTime.UtcNow;

            // Cleanup: remove expired flash sale items and reactivate products
            var expiredSales = await _db.FlashSales
                .Include(fs => fs.FlashSaleItems)
                    .ThenInclude(i => i.Product)
                .Where(fs => fs.EndTime < now)
                .ToListAsync();

            if (expiredSales.Any())
            {
                foreach (var sale in expiredSales)
                {
                    foreach (var item in sale.FlashSaleItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.IsActive = true;
                            item.Product.productType = "Normal";
                        }
                        _db.FlashSaleItems.Remove(item);
                    }
                }
                await _db.SaveChangesAsync();
            }

            var data = await _db.FlashSales
                                .Include(x => x.FlashSaleItems)
                                    .ThenInclude(i => i.Product)
                                        .ThenInclude(p => p.Images)
                                .Select(x => new FlashSaleDto
                                {
                                    FlashSaleId = x.FlashSaleId,
                                    Name = x.Name,
                                    StartTime = x.StartTime,
                                    EndTime = x.EndTime,
                                    FlashSaleItems = x.FlashSaleItems.Select(item => new FlashSaleItemDto
                                    {
                                        FlashSaleItemId = item.FlashSaleItemId,
                                        ProductId = item.ProductId,
                                        ProductName = item.Product.Name,
                                        DiscountPrice = item.DiscountPrice,
                                        OriginalPrice = item.Product.Price,
                                        ImageUrl = item.Product.Images.Select(img => img.ImageUrl).FirstOrDefault() ?? string.Empty,
                                        saleQuantity = item.saleQuantity,
                                        Sold = item.Sold
                                    }).ToList()
                                })
                                .ToListAsync();

            return data;
        }





        // ✅ Hàm duy nhất để tạo hoặc cập nhật Flash Sale
        public async Task<FlashSale> CreateOrUpdateFlashSaleAsync(int? id, FlashSaleCreateDTO dto)
        {
            try
            {
                // ✅ Lấy thời điểm hiện tại, cộng thêm thời gian được set (day/hours/minutes/seconds)
                var now = DateTime.UtcNow;
                int days = int.TryParse(dto.Day, out var d) ? d : 0;
                int hours = int.TryParse(dto.Hours, out var h) ? h : 0;
                int minutes = int.TryParse(dto.Minutes, out var m) ? m : 0;
                int seconds = int.TryParse(dto.Seconds, out var s) ? s : 0;

                var startTime = now;
                var endTime = now.AddDays(days)
                                 .AddHours(hours)
                                 .AddMinutes(minutes)
                                 .AddSeconds(seconds);

                // ✅ QUAN TRỌNG: Luôn luôn tìm bản ghi có Id = 1
                // Bỏ qua tham số 'id' đầu vào, chỉ tập trung vào số 1
                var flashSale = await _db.FlashSales.AsNoTracking().FirstOrDefaultAsync(x => x.FlashSaleId == 1);

                // ✅ Trường hợp 1: Chưa có Flash Sale nào (hoặc chưa có Id=1) -> TẠO MỚI
                if (flashSale == null)
                {
                    flashSale = new FlashSale
                    {
                        FlashSaleId = 1, // ⚠️ Ép cứng ID là 1
                        Name = dto.Name,
                        StartTime = startTime,
                        EndTime = endTime
                    };

                    // Lưu ý: Nếu database để tự động tăng (Identity), dòng này có thể cần cấu hình thêm
                    // Nhưng về mặt logic code C#, đây là cách gán ID = 1
                    _db.FlashSales.Add(flashSale);

                    // Mẹo: Với EF Core, nếu bạn gán ID thủ công cho cột Identity, 
                    // bạn có thể cần bật IDENTITY_INSERT trong SQL hoặc cấu hình DatabaseGeneratedOption.None
                }
                // ✅ Trường hợp 2: Đã có Id=1 -> CẬP NHẬT
                else
                {
                    // Vì đã dùng AsNoTracking ở trên để kiểm tra, giờ ta attach lại hoặc tìm lại để update
                    // Hoặc đơn giản là gán lại các giá trị và đánh dấu State = Modified

                    flashSale.Name = dto.Name;
                    flashSale.StartTime = startTime;
                    flashSale.EndTime = endTime;

                    _db.FlashSales.Update(flashSale);
                }

                await _db.SaveChangesAsync();
                return flashSale;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new Exception($"Invalid date/time: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating/updating flash sale: {ex.Message}");
            }
        }

        public async Task<bool> RemoveSaleProductAsync(int flashSaleItemId)
    {
        // 1. Tìm item thoả mãn cả 2 điều kiện:
        //    - Id của dòng đó đúng bằng flashSaleItemId
        //    - FlashSaleId (khóa ngoại) phải bằng 1
        var flashSaleItem = await _db.FlashSaleItems
             .Include(i => i.Product)
             .FirstOrDefaultAsync(x => x.ProductId == flashSaleItemId && x.FlashSaleId == 1);

        // 2. Nếu không tìm thấy (hoặc tìm thấy item nhưng nó không thuộc FlashSale 1)
        if (flashSaleItem == null)
        {
            return false; 
        }

        // 3. Đánh dấu để xóa
        _db.FlashSaleItems.Remove(flashSaleItem);

        // Reactivate product as normal
        if (flashSaleItem.Product != null)
        {
            flashSaleItem.Product.IsActive = true;
            flashSaleItem.Product.productType = "Normal";
        }

        // 4. Lưu thay đổi
        var rowsAffected = await _db.SaveChangesAsync();

        return rowsAffected > 0;
    }



        }
    }
