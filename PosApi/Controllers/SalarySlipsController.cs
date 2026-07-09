using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalarySlipsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalarySlipsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách phiếu lương đã chốt
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalarySlip>>> GetSalarySlips()
        {
            return await _context.SalarySlips.Include(s => s.Staff).ToListAsync();
        }

        // Chốt lương (Lưu phiếu mới)
        [HttpPost]
        public async Task<ActionResult<SalarySlip>> PostSalarySlip(SalarySlip slip)
        {
            // Kiểm tra xem tháng này người này đã chốt lương chưa, tránh bấm trùng 2 lần
            var existingSlip = await _context.SalarySlips.FirstOrDefaultAsync(s => s.StaffId == slip.StaffId && s.Month == slip.Month && s.Year == slip.Year);
            if (existingSlip != null)
            {
                return BadRequest("Nhân viên này đã được chốt lương trong tháng này rồi!");
            }

            slip.PaymentDate = DateTime.UtcNow;
            _context.SalarySlips.Add(slip);
            await _context.SaveChangesAsync();
            return Ok(slip);
        }
    }
}