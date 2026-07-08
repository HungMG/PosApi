using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendancesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendances()
        {
            return await _context.Attendances.Include(a => a.Staff).ToListAsync();
        }

        // API MỚI: Quản lý tự nhập số giờ làm bằng tay
        [HttpPost("manual")]
        public async Task<ActionResult<Attendance>> AddManualAttendance([FromBody] Attendance attendance)
        {
            // Ép kiểu giờ về chuẩn UTC để không bị lệch ngày
            attendance.WorkDate = attendance.WorkDate.ToUniversalTime();

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return Ok(attendance);
        }

        // API MỚI: Xóa dòng chấm công nếu quản lý lỡ nhập sai
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return NotFound();

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}