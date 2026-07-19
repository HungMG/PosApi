using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStaffs()
        {
            return await _context.Staffs.AsNoTracking().ToListAsync(); // 👉 Thêm ở đây
        }

        [HttpPost]
        public async Task<ActionResult<Staff>> PostStaff(Staff staff)
        {
            _context.Staffs.Add(staff);
            await _context.SaveChangesAsync();
            return Ok(staff);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStaff(int id, Staff staff)
        {
            if (id != staff.Id) return BadRequest();
            _context.Entry(staff).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();
            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // API HỖ TRỢ ĐĂNG NHẬP NHANH
        [HttpPost("login")]
        [HttpPost("login")]
        public async Task<ActionResult<Staff>> Login([FromBody] LoginDto loginDto)
        {
            var staff = await _context.Staffs
                .AsNoTracking() // 👉 Thêm ở đây để login nhanh hơn 1 chút
                .FirstOrDefaultAsync(s => s.Username == loginDto.Username && s.PasswordHash == loginDto.Password);

            if (staff == null || !staff.IsActive)
                return Unauthorized("Sai tài khoản, mật khẩu hoặc tài khoản đã bị khóa!");

            return Ok(staff);
        }
    }

    // Cái rổ hứng cục data đăng nhập
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}