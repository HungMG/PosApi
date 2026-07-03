using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Data;
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

        // API 1: Lấy danh sách toàn bộ Món ăn
        // Đường dẫn: GET /api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Lệnh Include(p => p.Category) cực kỳ quan trọng!
            // Nó giúp kết nối bảng Products với bảng Categories,
            // để khi trả về món ăn, bạn biết luôn món đó thuộc danh mục nào.
            var products = await _context.Products
                                         .Include(p => p.Category)
                                         .ToListAsync();
            return Ok(products);
        }

        // API 2: Thêm một Món ăn mới
        // Đường dẫn: POST /api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }
    }
}