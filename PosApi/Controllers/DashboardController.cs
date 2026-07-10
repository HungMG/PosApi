using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardDataDto>> GetDashboardSummary()
        {
            // 1. Xử lý Múi giờ Việt Nam (+7) để thống kê chính xác tuyệt đối
            DateTime utcNow = DateTime.UtcNow;
            DateTime vnNow = utcNow.AddHours(7);
            DateTime todayVn = vnNow.Date; // 00:00:00 hôm nay tại VN

            // Các mốc thời gian quy đổi ngược lại UTC để quét Database tốc độ cao
            DateTime startOfTodayUtc = todayVn.AddHours(-7);
            DateTime startOfMonthUtc = new DateTime(todayVn.Year, todayVn.Month, 1).AddHours(-7);
            DateTime startOf7DaysUtc = todayVn.AddDays(-6).AddHours(-7);

            // 2. TÍNH DOANH THU (Hôm nay & Tháng này)
            // 👉 ÉP KIỂU (decimal?) VÀ THÊM ?? 0 ĐỂ CHỐNG SẬP KHI KHÔNG CÓ ĐƠN NÀO
            var revenueToday = await _context.Orders
                .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.OrderDate >= startOfTodayUtc)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var revenueMonth = await _context.Orders
                .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.OrderDate >= startOfMonthUtc)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 3. TÍNH TỔNG CHI PHÍ THÁNG (Kho + Lương tạm tính)
            // 👉 LÀM TƯƠNG TỰ CHO BẢNG NHẬP KHO
            var inventoryCost = await _context.InventoryReceipts
                .Where(r => r.ImportDate >= startOfMonthUtc)
                .SumAsync(r => (decimal?)r.TotalCost) ?? 0;

            var attendances = await _context.Attendances
                .Include(a => a.Staff)
                .Where(a => a.WorkDate >= startOfMonthUtc)
                .ToListAsync();
            var salaryCost = attendances.Sum(a => (decimal)a.TotalHours * (a.Staff?.HourlyRate ?? 0));

            var totalCostMonth = inventoryCost + salaryCost;

            // 4. LỢI NHUẬN GỘP TRONG THÁNG
            var grossProfit = revenueMonth - totalCostMonth;

            // 5. THỐNG KÊ CHI TIẾT SẢN PHẨM BÁN RA (THÁNG NÀY)
            var orderDetailsThisMonth = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Include(od => od.Order)
                .Where(od => (od.Order.Status == "Paid" || od.Order.Status == "Completed") && od.Order.OrderDate >= startOfMonthUtc)
                .ToListAsync();

            var productSales = orderDetailsThisMonth
                .GroupBy(od => od.ProductId)
                .Select(g => new ProductSalesDto
                {
                    ProductName = g.First().Product?.Name ?? "Món đã xóa",
                    CategoryName = g.First().Product?.Category?.Name ?? "Khác",
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => (decimal)od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(p => p.TotalQuantity) // Xếp hạng bán chạy nhất lên đầu
                .ToList();

            // 6. CẢNH BÁO KẾT THÚC KHO
            var lowStockItems = await _context.Ingredients
                .Where(i => i.CurrentStock <= i.MinStock)
                .Select(i => new LowStockDto
                {
                    IngredientName = i.Name,
                    CurrentStock = i.CurrentStock,
                    MinStock = i.MinStock,
                    Unit = i.Unit
                })
                .ToListAsync();

            // 7. BIỂU ĐỒ DOANH THU 7 NGÀY GẦN NHẤT
            var recentOrders = await _context.Orders
                .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.OrderDate >= startOf7DaysUtc)
                .ToListAsync();

            var last7DaysRevenue = new List<DailyRevenueDto>();
            for (int i = 0; i < 7; i++)
            {
                var targetDateVn = todayVn.AddDays(-6 + i); // Tính tiến dần lên hôm nay
                last7DaysRevenue.Add(new DailyRevenueDto
                {
                    DateLabel = targetDateVn.ToString("dd/MM"),
                    Revenue = recentOrders.Where(o => o.OrderDate.AddHours(7).Date == targetDateVn).Sum(o => o.TotalAmount)
                });
            }

            // TRẢ VỀ TOÀN BỘ GÓI DỮ LIỆU ĐÃ TÍNH SẴN CHO WEB
            return Ok(new DashboardDataDto
            {
                RevenueToday = revenueToday,
                RevenueMonth = revenueMonth,
                TotalCostMonth = totalCostMonth,
                GrossProfit = grossProfit,
                ProductSales = productSales,
                LowStockItems = lowStockItems,
                Last7DaysRevenue = last7DaysRevenue
            });
        }
    }

    // ==========================================
    // CÁC LỚP "RỔ" HỨNG DỮ LIỆU ĐỂ TRẢ VỀ CHO WEB
    // ==========================================
    public class DashboardDataDto
    {
        public decimal RevenueToday { get; set; }
        public decimal RevenueMonth { get; set; }
        public decimal TotalCostMonth { get; set; }
        public decimal GrossProfit { get; set; }
        public List<ProductSalesDto> ProductSales { get; set; } = new();
        public List<LowStockDto> LowStockItems { get; set; } = new();
        public List<DailyRevenueDto> Last7DaysRevenue { get; set; } = new();
    }

    public class ProductSalesDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class LowStockDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public double CurrentStock { get; set; }
        public double MinStock { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class DailyRevenueDto
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}