using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // 1. Kéo danh sách món ăn
            var products = await _context.Products.ToListAsync();

            // 2. Kéo các nguyên liệu đóng chai (Sting, Nước suối) đang có liên kết
            var ingredients = await _context.Ingredients
                .Where(i => i.IsLinkedToProduct && i.LinkedProductId != null)
                .ToListAsync();

            // 3. Ghép số lượng Tồn Kho vào biến Quota để App Mobile hiển thị
            foreach (var p in products)
            {
                var linkedIng = ingredients.FirstOrDefault(i => i.LinkedProductId == p.Id);
                if (linkedIng != null)
                {
                    // Lấy số CurrentStock của Kho đè vào DailyQuota để App hiển thị
                    p.DailyQuota = (int)linkedIng.CurrentStock;
                }
            }

            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id) return BadRequest("ID món ăn không khớp!");

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}