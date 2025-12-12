using System.ComponentModel.DataAnnotations;
using EcomerceBE.Data;
using EcomerceBE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcomerceBE.Controllers
{
    [ApiController]
    [Route("api/shipping-methods")]
    public class ShippingMethodController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ShippingMethodController> _logger;

        public ShippingMethodController(AppDbContext context, ILogger<ShippingMethodController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/shipping-methods (Public - for checkout)
        [HttpGet]
        public async Task<IActionResult> GetShippingMethods()
        {
            var methods = await _context.ShippingMethods
                .Where(sm => sm.IsActive)
                .OrderBy(sm => sm.DisplayOrder)
                .ThenBy(sm => sm.Name)
                .Select(sm => new
                {
                    sm.ShippingMethodId,
                    sm.Name,
                    sm.Description,
                    sm.Price,
                    sm.EstimatedDays,
                    sm.DisplayOrder
                })
                .ToListAsync();

            return Ok(methods);
        }

        // GET /api/shipping-methods/admin (Admin - all methods including inactive)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllShippingMethods()
        {
            var methods = await _context.ShippingMethods
                .OrderBy(sm => sm.DisplayOrder)
                .ThenBy(sm => sm.Name)
                .Select(sm => new
                {
                    sm.ShippingMethodId,
                    sm.Name,
                    sm.Description,
                    sm.Price,
                    sm.EstimatedDays,
                    sm.IsActive,
                    sm.DisplayOrder,
                    sm.CreatedAt,
                    sm.UpdatedAt
                })
                .ToListAsync();

            return Ok(methods);
        }

        // GET /api/shipping-methods/admin/{id}
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetShippingMethodById(int id)
        {
            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.ShippingMethodId == id);

            if (method == null)
                return NotFound(new { message = "Shipping method not found." });

            return Ok(new
            {
                method.ShippingMethodId,
                method.Name,
                method.Description,
                method.Price,
                method.EstimatedDays,
                method.IsActive,
                method.DisplayOrder,
                method.CreatedAt,
                method.UpdatedAt
            });
        }

        // POST /api/shipping-methods/admin
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateShippingMethod([FromBody] CreateShippingMethodDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Name is required." });

            if (dto.Price < 0)
                return BadRequest(new { message = "Price must be greater than or equal to 0." });

            var method = new ShippingMethod
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                EstimatedDays = dto.EstimatedDays?.Trim(),
                IsActive = dto.IsActive ?? true,
                DisplayOrder = dto.DisplayOrder ?? 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShippingMethods.Add(method);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipping method created successfully.",
                shippingMethodId = method.ShippingMethodId,
                name = method.Name,
                price = method.Price
            });
        }

        // PATCH /api/shipping-methods/admin/{id}
        [HttpPatch("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShippingMethod(int id, [FromBody] UpdateShippingMethodDto dto)
        {
            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.ShippingMethodId == id);

            if (method == null)
                return NotFound(new { message = "Shipping method not found." });

            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                method.Name = dto.Name.Trim();
                hasChanges = true;
            }

            if (dto.Description != null)
            {
                method.Description = dto.Description.Trim();
                hasChanges = true;
            }

            if (dto.Price.HasValue)
            {
                if (dto.Price.Value < 0)
                    return BadRequest(new { message = "Price must be greater than or equal to 0." });
                method.Price = dto.Price.Value;
                hasChanges = true;
            }

            if (dto.EstimatedDays != null)
            {
                method.EstimatedDays = dto.EstimatedDays.Trim();
                hasChanges = true;
            }

            if (dto.IsActive.HasValue)
            {
                method.IsActive = dto.IsActive.Value;
                hasChanges = true;
            }

            if (dto.DisplayOrder.HasValue)
            {
                method.DisplayOrder = dto.DisplayOrder.Value;
                hasChanges = true;
            }

            if (!hasChanges)
                return Ok(new { message = "No changes detected.", shippingMethodId = method.ShippingMethodId });

            method.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipping method updated successfully.",
                shippingMethodId = method.ShippingMethodId,
                name = method.Name,
                price = method.Price
            });
        }

        // DELETE /api/shipping-methods/admin/{id}
        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShippingMethod(int id)
        {
            var method = await _context.ShippingMethods
                .FirstOrDefaultAsync(sm => sm.ShippingMethodId == id);

            if (method == null)
                return NotFound(new { message = "Shipping method not found." });

            // Check if any orders are using this shipping method
            var hasOrders = await _context.Orders
                .AnyAsync(o => o.ShippingMethodId == id);

            if (hasOrders)
            {
                // Soft delete - just deactivate
                method.IsActive = false;
                method.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Shipping method deactivated (has existing orders)." });
            }

            _context.ShippingMethods.Remove(method);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipping method deleted successfully." });
        }

        // DTOs
        public class CreateShippingMethodDto
        {
            [Required] public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            [Required] public decimal Price { get; set; }
            public string? EstimatedDays { get; set; }
            public bool? IsActive { get; set; }
            public int? DisplayOrder { get; set; }
        }

        public class UpdateShippingMethodDto
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public decimal? Price { get; set; }
            public string? EstimatedDays { get; set; }
            public bool? IsActive { get; set; }
            public int? DisplayOrder { get; set; }
        }
    }
}

