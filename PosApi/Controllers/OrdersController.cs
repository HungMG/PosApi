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
            // 2. Tạo Hóa Đơn
            var order = new Order
            {
                Note = dto.Note,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : "New", // Bắt trạng thái từ điện thoại gửi lên
                OrderDetails = orderDetails
            };

            // 3. Lưu vào Database Neon
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order); // Trả về thông tin hóa đơn cho App biết là thành công
        }
        // 1. HÀM XÓA ĐƠN HÀNG
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 2. HÀM SỬA ĐƠN HÀNG (Cập nhật lại món)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderCreateDto dto)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // Xóa sạch các món cũ trong đơn
            _context.OrderDetails.RemoveRange(order.OrderDetails);

            decimal totalAmount = 0;
            var newDetails = new List<OrderDetail>();

            // Tính tiền lại và gắn món mới vào
            foreach (var item in dto.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    newDetails.Add(new OrderDetail { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.Price });
                    totalAmount += (product.Price * item.Quantity);
                }
            }

            order.Note = dto.Note;
            order.Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : order.Status;
            order.TotalAmount = totalAmount;
            order.OrderDetails = newDetails;

            await _context.SaveChangesAsync();
            return Ok(order);
        }

        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "Paid"; // Đổi trạng thái thành Đã thanh toán
            await _context.SaveChangesAsync();
            return Ok();
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