using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Data;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Bơm (Inject) Database vào để Controller sử dụng
        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // API 1: Lấy danh sách toàn bộ Danh mục
        // Đường dẫn: GET /api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        // API 2: Thêm một Danh mục mới
        // Đường dẫn: POST /api/categories
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync(); // Lưu thẳng lên Neon.tech

            return Ok(category);
        }
    }
}