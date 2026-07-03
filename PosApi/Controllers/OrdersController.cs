using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Data;
using PosApi.DTOs;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // Bắn đơn hàng từ App lên Server
        [HttpPost]
        public async Task<ActionResult> CreateOrder(OrderRequestDto request)
        {
            // 1. Khởi tạo một đơn hàng mới (Trạng thái mặc định là "New")
            var newOrder = new Order
            {
                Note = request.Note,
                TotalAmount = 0
            };

            // 2. Duyệt qua từng món trong giỏ hàng do App gửi lên
            foreach (var item in request.CartItems)
            {
                // Truy vấn Database để lấy ĐÚNG giá gốc, chống gian lận/lỗi app
                var product = await _context.Products.FindAsync(item.ProductId);

                // Nếu món ăn không tồn tại hoặc đã bị khóa (hết hàng), bỏ qua
                if (product == null || !product.IsAvailable) continue;

                // 3. Tạo chi tiết đơn hàng
                var orderDetail = new OrderDetail
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price // Chốt giá ngay tại thời điểm đặt
                };

                newOrder.OrderDetails.Add(orderDetail);

                // 4. Cộng dồn tiền vào tổng hóa đơn
                newOrder.TotalAmount += (product.Price * item.Quantity);
            }

            // Nếu giỏ hàng rỗng (do lỗi hoặc chọn toàn món hết hàng) thì không lưu
            if (!newOrder.OrderDetails.Any()) return BadRequest("Giỏ hàng trống hoặc món đã hết.");

            // 5. Lưu toàn bộ xuống Neon.tech
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            return Ok(newOrder);
        }

        // API Lấy danh sách đơn hàng cho Web Admin xem
        [HttpGet]
        public async Task<ActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product) // Kéo theo cả tên món ăn ra để Admin xem cho dễ
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }
    }
}