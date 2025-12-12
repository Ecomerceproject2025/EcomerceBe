using EcomerceBE.Data;
using EcomerceBE.DTOs;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        public readonly AppDbContext _context;
        public ProductController(AppDbContext context)
        {
            _context= context;
        }


        [HttpGet("category")]
        public async Task<IActionResult> GetCateglory()
        {
            var result = await _context.Categories.ToListAsync();

            return Ok(result);
        }


        [HttpGet("category/{categoryId}/sizes")]
        public async Task<IActionResult> GetSizesByCategory(int categoryId)
        {
            var sizes = await _context.CategorySizes
                .Where(cs => cs.CategoryId == categoryId)
                .Include(cs => cs.Size) // đảm bảo load đầy đủ entity Size
                .Select(cs => new
                {
                    SizeId = cs.Size.SizeId,
                    Name = cs.Size.Name,
                    Description = cs.Size.Description
                })
                .ToListAsync();

            if (!sizes.Any())
                return NotFound(new { message = "No sizes found for this category." });

            return Ok(new { data = sizes });
        }





        [HttpGet("coupon_add_product_page")]
        public async Task<IActionResult> GetCouponsForProductAddPage()
        {
            var coupons = await _context.Coupons
                .Where(c => c.IsActive && (c.EndDate == null || c.EndDate > DateTime.UtcNow))
                .Select(c => new
                {
                    c.CouponId,
                    c.Code,
                    c.Description,
                    c.DiscountValue,
                    c.IsPercent,


                })
                .ToListAsync();
            return Ok(coupons);
        }






        [HttpPut("update_product/{id:int}")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] UpdateProductRequest request)
        {
            // --- VALIDATION ---
            if (request == null)
                return BadRequest(new { message = "Invalid payload." });

            // Validate Images
            if (request.ProductImages == null || !request.ProductImages.Any())
                return BadRequest(new { message = "Images required." });

            // Validate Variants
            if (request.Sizes == null || !request.Sizes.Any())
                return BadRequest(new { message = "Variants (sizes) required." });

            var name = (request.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Product title is required." });

            // Validate Prices
            if (request.Price <= 0)
                return BadRequest(new { message = "Price must be greater than 0." });

            if (request.ImportPrice < 0)
                return BadRequest(new { message = "Import price cannot be negative." });

            // Validate SalePrice relative to Price
            if (request.SalePrice.HasValue && request.SalePrice.Value > request.Price && request.SalePrice.Value > 0)
                return BadRequest(new { message = "Sale price cannot be higher than regular price." });

            // Validate ID khớp nhau
            if (!int.TryParse(request.Id, out int bodyId) || bodyId != id)
                return BadRequest(new { message = "Mismatched Product ID." });

            // --- CHECK UNIQUE NAME ---
            var existedName = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == name.ToLower() && p.ProductId != id);

            if (existedName)
                return Conflict(new { message = "Product name already exists." });

            // --- NORMALIZE VARIANTS ---
            var normalized = request.Sizes.Select((v, i) => new
            {
                Index = i,
                SizeType = (v.size?.type ?? "select").Trim().ToLowerInvariant(),
                SizeValueRaw = (v.size?.value ?? string.Empty).Trim(),
                ColorHex = (v.colorHex ?? "#000000").Trim().ToUpperInvariant(),
                Quantity = v.quantity < 1 ? 1 : v.quantity
            }).ToList();

            // Validate SizeType
            if (normalized.Any(v => v.SizeType != "select" && v.SizeType != "custom"))
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "Invalid size type. Must be 'select' or 'custom'.",
                    Extensions = { ["code"] = "InvalidSizeType" }
                });

            // Parse & Validate SizeId for 'select' type
            var selectItems = normalized
                .Where(v => v.SizeType == "select")
                .Select(v => new { v.Index, Parsed = int.TryParse(v.SizeValueRaw, out var pid) ? (int?)pid : null })
                .ToList();

            var parseErrors = selectItems.Where(x => x.Parsed is null).Select(x => x.Index).ToArray();
            if (parseErrors.Length > 0)
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "SizeId must be a valid integer.",
                    Extensions = { ["code"] = "FkMismatch", ["indices"] = parseErrors }
                });

            var sizeIds = selectItems.Select(x => x.Parsed!.Value).Distinct().ToArray();
            if (sizeIds.Length > 0)
            {
                // Check sizes exist
                var existingSizeIds = await _context.Set<Size>()
                    .Where(s => sizeIds.Contains(s.SizeId))
                    .Select(s => s.SizeId)
                    .ToListAsync();

                var missing = sizeIds.Except(existingSizeIds).ToArray();
                if (missing.Length > 0)
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Some SizeId do not exist.",
                        Extensions = { ["code"] = "FkMismatch", ["missing"] = missing }
                    });

                // Check size belongs to category
                var validCategorySizes = await _context.Set<CategorySize>()
                    .Where(cs => cs.CategoryId == request.CategoryId && sizeIds.Contains(cs.SizeId))
                    .Select(cs => cs.SizeId)
                    .ToListAsync();

                var notInCategory = sizeIds.Except(validCategorySizes).ToArray();
                if (notInCategory.Length > 0)
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Some SizeId do not belong to the selected category.",
                        Extensions = new Dictionary<string, object?> { ["code"] = "CategorySizeMismatch", ["sizeIds"] = notInCategory }
                    });
            }

            // Check duplicate (size + color)
            var keyWithIndex = normalized.Select(v => new
            {
                v.Index,
                Key = v.SizeType == "select"
                    ? $"select:{v.SizeValueRaw}|{v.ColorHex}"
                    : $"custom:{v.SizeValueRaw}|{v.ColorHex}"
            }).ToList();

            var duplicates = keyWithIndex.GroupBy(x => x.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(x => x.Index).OrderBy(i => i).ToArray())
                .ToList();

            if (duplicates.Count > 0)
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "Duplicate color for the same size.",
                    Extensions = { ["code"] = "DuplicateVariant", ["indices"] = duplicates }
                });

            // --- FETCH ENTITY ---
            var product = await _context.Products
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.ProductColors)
                .Include(p => p.Images)
                .Include(p => p.ProductCoupons)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            // --- UPDATE SCALAR PROPERTIES ---F
            product.Name = request.Title ?? string.Empty;
            product.Description = request.Description ?? string.Empty;
            product.Price = request.Price;
            product.ImportPrice = request.ImportPrice;

        

            product.ReturnDeliveryDay = request.ReturnDeliveryDay;
            product.CategoryId = request.CategoryId;

            // --- UPDATE IMAGES ---
            product.Images.Clear();

            // Thêm hero image nếu có
            if (!string.IsNullOrEmpty(request.HeroImage))
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = request.HeroImage,

                });
            }

            // Thêm các ảnh khác
            foreach (var imgUrl in request.ProductImages)
            {
                product.Images.Add(new ProductImage { ImageUrl = imgUrl });
            }

            // --- UPDATE VARIANTS ---
            product.ProductSizes.Clear();

            var sizeCache = new Dictionary<string, ProductSize>();
            foreach (var v in normalized)
            {
                string sizeKey;
                int? sizeId = null;
                string? customValue = null;

                if (v.SizeType == "select")
                {
                    if (int.TryParse(v.SizeValueRaw, out int parsedId))
                    {
                        sizeId = parsedId;
                        sizeKey = $"select:{sizeId}";
                    }
                    else
                        continue;
                }
                else
                {
                    customValue = v.SizeValueRaw;
                    sizeKey = $"custom:{customValue}";
                }

                if (!sizeCache.TryGetValue(sizeKey, out var ps))
                {
                    ps = new ProductSize
                    {
                        Product = product,
                        SizeId = sizeId,
                        CustomValue = customValue,
                        ProductColors = new List<ProductColor>()
                    };
                    product.ProductSizes.Add(ps);
                    sizeCache[sizeKey] = ps;
                }

                ps.ProductColors.Add(new ProductColor
                {
                    ProductSize = ps,
                    ColorCode = v.ColorHex,
                    Quantity = v.Quantity
                });
            }

            // Cập nhật tổng kho
            product.StockQuantity = normalized.Sum(v => v.Quantity);
            product.productType = request.productType ?? "Normal";
            product.IsActive = request.productType?.Equals("flashsale", StringComparison.OrdinalIgnoreCase) == true
                ? false
                : true;

            // ✅ Kiểm tra product đã tồn tại trong FlashSale(id=1)
            var existingSaleItem = await _context.Set<FlashSaleItem>()
                .FirstOrDefaultAsync(si => si.ProductId == id && si.FlashSaleId == 1);

            if (request.productType?.Equals("flashsale", StringComparison.OrdinalIgnoreCase) == true)
            {
                // ✅ Nếu chưa tồn tại => Tạo FlashSaleItem mới
                if (existingSaleItem == null)
                {
                    var saleItem = new FlashSaleItem
                    {
                        FlashSaleId = 1,
                        ProductId = product.ProductId,
                        Product = product,
                        DiscountPrice = request.SalePrice!.Value,
                        saleQuantity = request.saleQuantity!.Value
                    };

                    _context.Set<FlashSaleItem>().Add(saleItem);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // ✅ Nếu đã tồn tại => Cập nhật giá và số lượng
                    existingSaleItem.DiscountPrice = request.SalePrice!.Value;
                    existingSaleItem.saleQuantity = request.saleQuantity!.Value;

                    _context.Set<FlashSaleItem>().Update(existingSaleItem);
                    await _context.SaveChangesAsync();
                }
            }
            else if (request.productType?.Equals("normal", StringComparison.OrdinalIgnoreCase) == true)
            {
                // ✅ Nếu là normal => Xóa khỏi FlashSaleItem
                if (existingSaleItem != null)
                {
                    _context.Set<FlashSaleItem>().Remove(existingSaleItem);
                    await _context.SaveChangesAsync();
                }
            }



            // --- SAVE ---
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "Update success",
                    id = product.ProductId,
                    data = new
                    {
                        name = product.Name,
                        price = product.Price,
                    
                        stockQuantity = product.StockQuantity
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                var root = ex.InnerException?.Message ?? ex.Message;

                if (root.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    root.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Duplicate value detected in database.",
                        Extensions = { ["code"] = "DuplicateVariant" }
                    });
                }

                return StatusCode(500, new ProblemDetails
                {
                    Status = 500,
                    Title = "Database error",
                    Detail = root
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product.", error = ex.Message });
            }
        }




        [HttpPost("add_product")]

        public async Task<IActionResult> AddProduct([FromBody] AddProduct request)
        {
            // --- VALIDATION ---
            if (request == null)
                return BadRequest(new { message = "Invalid payload." });

            // Validate Images
            if (request.Images == null || !request.Images.Any())
                return BadRequest(new { message = "Images required." });

            // Validate Variants
            if (request.Variants == null || !request.Variants.Any())
                return BadRequest(new { message = "Variants (sizes) required." });

            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Product name is required." });

            // Validate Prices
            if (request.Price <= 0)
                return BadRequest(new { message = "Price must be greater than 0." });
            if (request.ImportPrice < 0)
                return BadRequest(new { message = "Import price cannot be negative." });
            if (request.SalePrice > request.Price && request.SalePrice > 0)
                return BadRequest(new { message = "Sale price cannot be higher than the regular price." });

            // Check name uniqueness
            var existedName = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == name.ToLower());
            if (existedName)
                return Conflict(new { message = "Product name already exists." });

            // --- NORMALIZE VARIANTS ---
            var normalized = request.Variants.Select((v, i) => new
            {
                Index = i,
                SizeType = (v.size?.type ?? "select").Trim().ToLowerInvariant(), // "select" hoặc "custom"
                SizeValueRaw = (v.size?.value ?? string.Empty).Trim(),
                ColorHex = (v.colorHex ?? "#000000").Trim().ToUpperInvariant(),
                Quantity = v.quantity < 1 ? 1 : v.quantity
            }).ToList();

            // Validate SizeType
            if (normalized.Any(v => v.SizeType != "select" && v.SizeType != "custom"))
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "Invalid size type. Must be 'select' or 'custom'.",
                    Extensions = { ["code"] = "InvalidSizeType" }
                });

            // Parse & validate SizeId for 'select' type
            var selectItems = normalized
                .Where(v => v.SizeType == "select")
                .Select(v => new { v.Index, Parsed = int.TryParse(v.SizeValueRaw, out var pid) ? (int?)pid : null })
                .ToList();

            var parseErrors = selectItems.Where(x => x.Parsed is null).Select(x => x.Index).ToArray();
            if (parseErrors.Length > 0)
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "SizeId must be a valid integer.",
                    Extensions = { ["code"] = "FkMismatch", ["indices"] = parseErrors }
                });

            var sizeIds = selectItems.Select(x => x.Parsed!.Value).Distinct().ToArray();

            if (sizeIds.Length > 0)
            {
                // Kiểm tra size tồn tại
                var existingSizeIds = await _context.Set<Size>()
                    .Where(s => sizeIds.Contains(s.SizeId))
                    .Select(s => s.SizeId)
                    .ToListAsync();

                var missing = sizeIds.Except(existingSizeIds).ToArray();
                if (missing.Any())
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Some SizeId do not exist.",
                        Extensions = { ["code"] = "FkMismatch", ["missing"] = missing }
                    });

                // Kiểm tra size thuộc category
                var validCategorySizes = await _context.Set<CategorySize>()
                    .Where(cs => cs.CategoryId == request.CategoryId && sizeIds.Contains(cs.SizeId))
                    .Select(cs => cs.SizeId)
                    .ToListAsync();

                var notInCategory = sizeIds.Except(validCategorySizes).ToArray();
                if (notInCategory.Any())
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Some SizeId do not belong to the selected category.",
                        Extensions = { ["code"] = "CategorySizeMismatch", ["sizeIds"] = notInCategory }
                    });
            }

            // Kiểm tra duplicate (size + color)
            var keyWithIndex = normalized.Select(v => new
            {
                v.Index,
                Key = v.SizeType == "select"
                    ? $"select:{v.SizeValueRaw}|{v.ColorHex}"
                    : $"custom:{v.SizeValueRaw}|{v.ColorHex}"
            }).ToList();

            var duplicates = keyWithIndex.GroupBy(x => x.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(x => x.Index).OrderBy(i => i).ToArray())
                .ToList();

            if (duplicates.Any())
                return Conflict(new ProblemDetails
                {
                    Status = 409,
                    Title = "Conflict",
                    Detail = "Duplicate color for the same size.",
                    Extensions = { ["code"] = "DuplicateVariant", ["indices"] = duplicates }
                });

            // --- TẠO ENTITY MỚI ---
            var product = new Product
            {
                Name = name,
                Description = request.Description,
                Price = request.Price,
                ImportPrice = request.ImportPrice,
                ReturnDeliveryDay = request.ReturnDeliveryDay,
                CategoryId = request.CategoryId,
                Images = new List<ProductImage>(),
                ProductSizes = new List<ProductSize>(),
                ProductCoupons = new List<ProductCoupon>(),
            };

            // Thêm ảnh hero nếu có
            if (request.Images != null && request.Images.Any())
            {
                var firstImage = request.Images.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstImage))
                {
                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = firstImage
                    });
                }

                // Thêm các ảnh còn lại
                foreach (var imgUrl in request.Images)
                {
                    if (!string.IsNullOrEmpty(imgUrl) && imgUrl != firstImage)
                    {
                        product.Images.Add(new ProductImage { ImageUrl = imgUrl });
                    }
                }
            }

            // --- Thêm variants (sizes + colors) ---
            var sizeCache = new Dictionary<string, ProductSize>();
            foreach (var v in normalized)
            {
                string sizeKey;
                int? sizeId = null;
                string? customValue = null;

                if (v.SizeType == "select")
                {
                    if (int.TryParse(v.SizeValueRaw, out int parsedId))
                    {
                        sizeId = parsedId;
                        sizeKey = $"select:{sizeId}";
                    }
                    else continue; // bỏ qua nếu không parse được
                }
                else
                {
                    customValue = v.SizeValueRaw;
                    sizeKey = $"custom:{customValue}";
                }

                if (!sizeCache.TryGetValue(sizeKey, out var ps))
                {
                    ps = new ProductSize
                    {
                        Product = product,
                        SizeId = sizeId,
                        CustomValue = customValue,
                        ProductColors = new List<ProductColor>()
                    };
                    product.ProductSizes.Add(ps);
                    sizeCache[sizeKey] = ps;
                }

                // Thêm màu cho size
                ps.ProductColors.Add(new ProductColor
                {
                    ProductSize = ps,
                    ColorCode = v.ColorHex,
                    Quantity = v.Quantity
                });
            }


            // Cập nhật tổng kho
            product.StockQuantity = normalized.Sum(v => v.Quantity);
            product.productType = request.productType ?? "Normal";
            product.IsActive = request.productType?.Equals("flashsale", StringComparison.OrdinalIgnoreCase) == true
                ? false
                : true;
            if (request.productType?.Equals("flashsale", StringComparison.OrdinalIgnoreCase) == true)
            {
                var saleItem = new FlashSaleItem
                {
                    FlashSaleId = 1,  // ✅ Cố định FlashSaleId = 1
                    ProductId = product.ProductId,
                    Product = product,

                    DiscountPrice  = request.SalePrice!, 
                    saleQuantity = request.saleQuantity      // Nếu có
                };
                _context.Set<FlashSaleItem>().Add(saleItem);
                await _context.SaveChangesAsync();
            }
            // --- Lưu vào database ---
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Add success",
                    id = product.ProductId,
                    data = new
                    {
                        name = product.Name,
                        price = product.Price,
                      
                        stockQuantity = product.StockQuantity
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                var rootMsg = ex.InnerException?.Message ?? ex.Message;
                if (rootMsg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    rootMsg.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new ProblemDetails
                    {
                        Status = 409,
                        Title = "Conflict",
                        Detail = "Duplicate value detected in database.",
                        Extensions = { ["code"] = "DuplicateVariant" }
                    });
                }

                return StatusCode(500, new ProblemDetails
                {
                    Status = 500,
                    Title = "Database error",
                    Detail = rootMsg
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the product.", error = ex.Message });
            }
        }







        [HttpPost("import-products")]
        public async Task<IActionResult> ImportProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File is required." });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only .xlsx files are supported." });

            var errors = new List<string>();
            var successProducts = new List<object>();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension?.Rows ?? 0;

                        if (rowCount < 2)
                            return BadRequest(new { message = "Excel file must have header and at least one data row." });

                        // Parse từng row
                        for (int row = 2; row <= rowCount; row++)
                        {
                            try
                            {
                                var name = (worksheet.Cells[row, 1].Value?.ToString() ?? string.Empty).Trim();
                                var description = worksheet.Cells[row, 2].Value?.ToString() ?? string.Empty;
                                var importPrice = decimal.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out var ip) ? ip : 0;
                                var price = decimal.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out var p) ? p : 0;
                                var salePrice = decimal.TryParse(worksheet.Cells[row, 5].Value?.ToString(), out var sp) ? sp : 0;
                                var returnDeliveryDay = int.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out var rdd) ? rdd : (int?)null;
                                var categoryId = worksheet.Cells[row, 7].Value?.ToString() ?? string.Empty;
                                var imagesCSV = worksheet.Cells[row, 8].Value?.ToString() ?? string.Empty;
                                var listCouponCSV = worksheet.Cells[row, 9].Value?.ToString() ?? string.Empty;
                                var variantsCSV = worksheet.Cells[row, 10].Value?.ToString() ?? string.Empty;
                                var productType = worksheet.Cells[row, 11].Value?.ToString() ?? string.Empty;
                                // Parse Images
                                var images = string.IsNullOrWhiteSpace(imagesCSV)
                                    ? new List<string>()
                                    : imagesCSV.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

                                // Parse Variants: format "type:value:#color:qty|type:value:#color:qty|..."
                                var variants = new List<VariantVm>();
                                if (!string.IsNullOrWhiteSpace(variantsCSV))
                                {
                                    var variantItems = variantsCSV.Split('|');
                                    foreach (var item in variantItems)
                                    {
                                        var parts = item.Split(':');
                                        if (parts.Length == 4)
                                        {
                                            var type = parts[0].Trim();
                                            var value = parts[1].Trim();
                                            var color = parts[2].Trim();
                                            var qty = int.TryParse(parts[3], out int q) ? q : 1;

                                            if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(color))
                                            {
                                                variants.Add(new VariantVm
                                                {
                                                    size = new SizeVm { type = type, value = value },
                                                    colorHex = color,
                                                    quantity = qty < 1 ? 1 : qty
                                                });
                                            }
                                        }
                                    }
                                }

                                // Tạo request AddProduct từ dữ liệu Excel
                                var addProductRequest = new AddProduct
                                {
                                    Name = name,
                                    Description = description,
                                    ImportPrice = importPrice,
                                    Price = price,
                                    SalePrice = salePrice,
                                    ReturnDeliveryDay = returnDeliveryDay,
                                    CategoryId = int.Parse(categoryId),
                                    Images = images,
                                    Variants = variants,
                                    coupons = new List<Coupon>() ,// Xử lý coupons nếu cần
                                     productType = productType,
                                };

                                // Parse Coupons và thêm vào request: format "CODE:Description:Discount:IsActive|..."
                                if (!string.IsNullOrWhiteSpace(listCouponCSV))
                                {
                                    var couponItems = listCouponCSV.Split('|');
                                    foreach (var couponItem in couponItems)
                                    {
                                        var parts = couponItem.Split(':');
                                        if (parts.Length >= 4)
                                        {
                                            var code = parts[0].Trim();
                                            var desc = parts[1].Trim();
                                            var discount = decimal.TryParse(parts[2], out var d) ? d : 0;
                                            var isActive = int.TryParse(parts[3], out var ia) ? ia == 1 : false;

                                            // Kiểm tra coupon đã tồn tại
                                            var existingCoupon = await _context.Set<Coupon>()
                                                .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());

                                            if (existingCoupon != null)
                                            {
                                                addProductRequest.coupons.Add(existingCoupon);
                                            }
                                            else
                                            {
                                                var newCoupon = new Coupon
                                                {
                                                    Code = code,
                                                    Description = desc,
                                                    DiscountValue = discount,
                                                    IsActive = isActive
                                                };
                                                addProductRequest.coupons.Add(newCoupon);
                                            }
                                        }
                                    }
                                }

                                // Gọi AddProduct để validate và lưu
                                var result = await AddProduct(addProductRequest);

                                if (result is OkObjectResult okResult && okResult.Value != null)
                                {
                                    successProducts.Add(okResult.Value);
                                }
                                else if (result is ConflictObjectResult conflictResult)
                                {
                                    var problemDetails = conflictResult.Value as ProblemDetails;
                                    errors.Add($"Row {row}: {problemDetails?.Detail ?? "Conflict error"}");
                                }
                                else if (result is BadRequestObjectResult badResult && badResult.Value != null)
                                {
                                    dynamic badValue = badResult.Value;
                                    errors.Add($"Row {row}: {badValue?.message ?? "Bad request"}");
                                }
                                else
                                {
                                    errors.Add($"Row {row}: Unknown error occurred.");
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Row {row}: {ex.Message}");
                            }
                        }

                        return Ok(new
                        {
                            message = "Import completed",
                            summary = new
                            {
                                successCount = successProducts.Count,
                                errorCount = errors.Count,
                                totalRows = rowCount - 1
                            },
                            errors = errors.Any() ? errors : null,
                            successProducts = successProducts.Any() ? successProducts : null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during import.", error = ex.Message });
            }
        }

    }
}




