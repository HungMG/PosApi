using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShiftReportsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DÀNH CHO WEB ADMIN: Kéo toàn bộ lịch sử Chốt ca để kiểm tra
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftReport>>> GetShiftReports()
        {
            return await _context.ShiftReports
                .AsNoTracking() // 👉 Thêm dòng này
                .Include(s => s.Staff)
                .OrderByDescending(s => s.ReportDate)
                .ToListAsync();
        }

        // 2. DÀNH CHO APP MOBILE: Nhân viên bấm Chốt Ca bắn dữ liệu lên đây
        [HttpPost]
        public async Task<ActionResult<ShiftReport>> PostShiftReport(ShiftReport report)
        {
            report.ReportDate = report.ReportDate.ToUniversalTime();

            // 👉 SỬA LẠI CÔNG THỨC: Chỉ tính chênh lệch dựa trên Tiền Mặt
            // Tiền lệch = Tiền đếm được - (Tiền mồi đầu ca + Doanh thu tiền mặt)
            report.CashDifference = report.ActualCashAmount - (report.StartingCash + report.SystemCashAmount);

            _context.ShiftReports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(report);
        }

        // 3. DÀNH CHO WEB ADMIN: Quản lý bấm Duyệt / Xác nhận báo cáo
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            var report = await _context.ShiftReports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = "Approved";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ==========================================
        // 4. DÀNH CHO WEB ADMIN: Xóa phiếu chốt ca (dọn rác/nhập sai)
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShiftReport(int id)
        {
            var report = await _context.ShiftReports.FindAsync(id);
            if (report == null)
                return NotFound("Không tìm thấy phiếu chốt ca này!");

            // Xóa vĩnh viễn khỏi Database
            _context.ShiftReports.Remove(report);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}