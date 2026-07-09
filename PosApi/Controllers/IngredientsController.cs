using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IngredientsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách nguyên liệu trong kho
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
        {
            return await _context.Ingredients.ToListAsync();
        }

        // TẠO MỚI (CÓ KÈM LOGIC TỰ TẠO SẢN PHẨM)
        [HttpPost]
        public async Task<ActionResult<Ingredient>> CreateIngredient([FromBody] IngredientCreateDto dto)
        {
            // Bắt đầu Transaction để bảo vệ an toàn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo Nguyên liệu trước
                var ingredient = new Ingredient
                {
                    Name = dto.Name,

                    // 👉 BỔ SUNG DÒNG NÀY ĐỂ LƯU VÀO DATABASE
                    Category = string.IsNullOrWhiteSpace(dto.Category) ? "Khác" : dto.Category,

                    Unit = dto.Unit,
                    CurrentStock = dto.CurrentStock,
                    MinStock = dto.MinStock,
                    CostPrice = dto.CostPrice,
                    IsLinkedToProduct = dto.IsLinkedToProduct
                };

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync(); // Lưu phát đầu để lấy cái ID nguyên liệu

                // 2. Nếu có tick chọn "Bán trực tiếp" và có điền Giá + Danh mục
                if (dto.IsLinkedToProduct && dto.SellingPrice.HasValue && dto.CategoryId.HasValue)
                {
                    var product = new Product
                    {
                        Name = ingredient.Name,
                        Price = dto.SellingPrice.Value,
                        IsAvailable = ingredient.CurrentStock > 0, // Nếu vừa tạo mà kho > 0 thì mở bán luôn
                        CategoryId = dto.CategoryId.Value,
                        ImageUrl = ""
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync(); // Lưu phát hai để lấy ID sản phẩm

                    // 3. Vòng ngược lại, gắn ID sản phẩm vào nguyên liệu
                    ingredient.LinkedProductId = product.Id;
                    await _context.SaveChangesAsync();
                }

                // Thành công trót lọt 100% mới chốt sổ (Commit)
                await transaction.CommitAsync();
                return Ok(ingredient);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Có lỗi là hủy sạch
                return StatusCode(500, "Lỗi hệ thống khi tạo liên kết: " + ex.Message);
            }
        }

        // CẬP NHẬT (KÈM LOGIC AUTO-LOCK KHI HẾT HÀNG)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIngredient(int id, [FromBody] Ingredient ingredient)
        {
            if (id != ingredient.Id) return BadRequest();

            _context.Entry(ingredient).State = EntityState.Modified;

            // Kiểm tra: Nếu nguyên liệu này có link với Sản phẩm
            if (ingredient.IsLinkedToProduct && ingredient.LinkedProductId.HasValue)
            {
                var product = await _context.Products.FindAsync(ingredient.LinkedProductId.Value);
                if (product != null)
                {
                    // Tự động Tắt/Mở bán dựa vào số lượng tồn kho
                    product.IsAvailable = ingredient.CurrentStock > 0;
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // XÓA NGUYÊN LIỆU
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- CÁI "RỔ" HỨNG DỮ LIỆU TỪ WEB GỬI LÊN ---
        public class IngredientCreateDto
        {
            public string Name { get; set; } = string.Empty;

            // 👉 BỔ SUNG DÒNG NÀY ĐỂ HỨNG DỮ LIỆU
            public string Category { get; set; } = "Khác";
            public string Unit { get; set; } = string.Empty;
            public double CurrentStock { get; set; }
            public double MinStock { get; set; }
            public decimal CostPrice { get; set; }

            public bool IsLinkedToProduct { get; set; }
            // 2 Biến phụ này chỉ có tác dụng tạo Sản phẩm, không lưu vào bảng Kho
            public decimal? SellingPrice { get; set; }
            public int? CategoryId { get; set; }
        }
    }
}