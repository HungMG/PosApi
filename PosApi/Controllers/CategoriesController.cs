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
        // API 3: Sửa một Danh mục
        // Đường dẫn: PUT /api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest("ID danh mục không khớp!");
            }

            // Báo cho Database biết là object này đã bị thay đổi
            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync(); // Đẩy cập nhật lên Neon.tech
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Categories.Any(e => e.Id == id))
                    return NotFound("Không tìm thấy danh mục để sửa.");
                else
                    throw;
            }

            return NoContent(); // Thành công thì không cần trả về gì (Status 204)
        }

        // API 4: Xóa một Danh mục
        // Đường dẫn: DELETE /api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục để xóa.");
            }

            // Xóa khỏi Database
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync(); // Đẩy cập nhật lên Neon.tech

            return NoContent(); // Thành công (Status 204)
        }
    }
}