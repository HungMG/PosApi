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
            try
            {
                // 1. Xử lý Múi giờ Việt Nam (+7)
                DateTime utcNow = DateTime.UtcNow;
                DateTime vnNow = utcNow.AddHours(7);
                DateTime todayVn = vnNow.Date; // 00:00:00 hôm nay tại VN

                // 👉 SỬA LỖI Ở ĐÂY: Dùng DateTime.SpecifyKind để ÉP BUỘC dán nhãn UTC cho PostgreSQL hiểu!
                DateTime startOfTodayUtc = DateTime.SpecifyKind(todayVn.AddHours(-7), DateTimeKind.Utc);
                DateTime startOfMonthUtc = DateTime.SpecifyKind(new DateTime(todayVn.Year, todayVn.Month, 1).AddHours(-7), DateTimeKind.Utc);
                DateTime startOf7DaysUtc = DateTime.SpecifyKind(todayVn.AddDays(-6).AddHours(-7), DateTimeKind.Utc);

                // ========================================================
                // 2. KÉO DATA VỀ RAM TRƯỚC ĐỂ TRÁNH LỖI SUMASYNC CỦA DATABASE
                // ========================================================

                // Kéo các đơn đã thanh toán trong tháng này về RAM
                var ordersMonth = await _context.Orders
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
                    .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.OrderDate >= startOfMonthUtc)
                    .ToListAsync();

                // Dùng C# tính tổng (An toàn tuyệt đối)
                var revenueToday = ordersMonth
                    .Where(o => o.OrderDate >= startOfTodayUtc)
                    .Sum(o => o.TotalAmount);

                var revenueMonth = ordersMonth.Sum(o => o.TotalAmount);

                // 3. TÍNH TỔNG CHI PHÍ THÁNG (Kéo về RAM rồi mới tính)
                var receiptsMonth = await _context.InventoryReceipts
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
                    .Where(r => r.ImportDate >= startOfMonthUtc)
                    .ToListAsync();
                var inventoryCost = receiptsMonth.Sum(r => r.TotalCost);

                var attendances = await _context.Attendances
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
                    .Include(a => a.Staff)
                    .Where(a => a.WorkDate >= startOfMonthUtc)
                    .ToListAsync();
                var salaryCost = attendances.Sum(a => (decimal)a.TotalHours * (a.Staff?.HourlyRate ?? 0));

                var totalCostMonth = inventoryCost + salaryCost;

                // 4. LỢI NHUẬN GỘP TRONG THÁNG
                var grossProfit = revenueMonth - totalCostMonth;

                // 5. THỐNG KÊ CHI TIẾT SẢN PHẨM BÁN RA
                var orderDetailsThisMonth = await _context.OrderDetails
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
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
                    .OrderByDescending(p => p.TotalQuantity)
                    .ToList();

                // 6. CẢNH BÁO KẾT THÚC KHO
                var lowStockItems = await _context.Ingredients
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
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
                    .AsNoTracking() // 👉 CHÈN VÀO ĐÂY
                    .Where(o => (o.Status == "Paid" || o.Status == "Completed") && o.OrderDate >= startOf7DaysUtc)
                    .ToListAsync();

                var last7DaysRevenue = new List<DailyRevenueDto>();
                for (int i = 0; i < 7; i++)
                {
                    var targetDateVn = todayVn.AddDays(-6 + i);
                    last7DaysRevenue.Add(new DailyRevenueDto
                    {
                        DateLabel = targetDateVn.ToString("dd/MM"),
                        Revenue = recentOrders.Where(o => o.OrderDate.AddHours(7).Date == targetDateVn).Sum(o => o.TotalAmount)
                    });
                }

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
            catch (Exception ex)
            {
                // Nếu vẫn xui xẻo văng lỗi, nó sẽ in thẳng ra câu lỗi để sếp xem dễ dàng!
                return StatusCode(500, $"Lỗi API Dashboard: {ex.Message}");
            }
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