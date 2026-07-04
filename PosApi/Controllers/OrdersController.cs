using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Data;
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

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (dto.CartItems == null || !dto.CartItems.Any())
            {
                return BadRequest("Giỏ hàng đang trống!");
            }

            decimal totalAmount = 0;
            var orderDetails = new List<OrderDetail>();

            // 1. Quét từng món App gửi lên để dò giá gốc dưới DB (Bảo mật, chống hack giá)
            foreach (var item in dto.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price // Chốt giá tại thời điểm bán
                    });

                    totalAmount += (product.Price * item.Quantity);
                }
            }

            // 2. Tạo Hóa Đơn (Áp dụng đúng Model của bạn)
            var order = new Order
            {
                Note = dto.Note,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                // Status = "New" -> Thuộc tính này sẽ tự động được gán mặc định như bạn đã code trong Model
                OrderDetails = orderDetails
            };

            // 3. Lưu vào Database Neon
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order); // Trả về thông tin hóa đơn cho App biết là thành công
        }

        // (Tặng kèm) API Lấy danh sách hóa đơn để mốt làm tab "Lịch sử đơn hàng"
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }
    }
}