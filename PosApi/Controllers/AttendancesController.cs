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

        // API Check-in (Vào ca)
        [HttpPost("checkin/{staffId}")]
        public async Task<ActionResult<Attendance>> CheckIn(int staffId)
        {
            var attendance = new Attendance
            {
                StaffId = staffId,
                WorkDate = DateTime.UtcNow.Date,
                CheckInTime = DateTime.UtcNow
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return Ok(attendance);
        }

        // API Check-out (Ra ca & Tự tính giờ)
        [HttpPut("checkout/{id}")]
        public async Task<IActionResult> CheckOut(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null || attendance.CheckOutTime != null)
                return BadRequest("Không tìm thấy ca làm hoặc đã check-out rồi!");

            attendance.CheckOutTime = DateTime.UtcNow;

            // Tính tổng số giờ làm việc
            if (attendance.CheckInTime.HasValue)
            {
                TimeSpan timeWorked = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
                attendance.TotalHours = Math.Round(timeWorked.TotalHours, 2);
            }

            await _context.SaveChangesAsync();
            return Ok(attendance);
        }
    }
}