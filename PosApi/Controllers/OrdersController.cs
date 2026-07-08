using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PosApi.Models;
using PosApi.Hubs;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrdersController(AppDbContext context, IHubContext<OrderHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (dto.CartItems == null || !dto.CartItems.Any())
                return BadRequest("Giỏ hàng đang trống!");

            decimal totalAmount = 0;
            var orderDetails = new List<OrderDetail>();

            foreach (var item in dto.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // 1. Kiểm tra món này có bị hết hàng không?
                    if (!product.IsAvailable)
                        return BadRequest($"Món {product.Name} đã hết hàng!");

                    // 2. Trừ Quota (nếu món đó có thiết lập giới hạn bán trong ngày)
                    if (product.DailyQuota.HasValue)
                    {
                        if (product.DailyQuota.Value < item.Quantity)
                            return BadRequest($"Món {product.Name} chỉ còn {product.DailyQuota.Value} phần!");

                        product.DailyQuota -= item.Quantity; // Trừ số lượng

                        // Nếu bán hết quota, tự động tắt hiển thị món đó luôn
                        if (product.DailyQuota <= 0)
                        {
                            product.DailyQuota = 0;
                            product.IsAvailable = false;
                        }
                    }

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    });

                    totalAmount += (product.Price * item.Quantity);
                }
            }

            var order = new Order
            {
                Note = dto.Note,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : "Pending",
                OrderType = !string.IsNullOrEmpty(dto.OrderType) ? dto.OrderType : "DineIn", // Bắt DineIn/TakeAway
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderChanged");

            return Ok(order);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // Nếu hủy đơn, trả lại Quota cho các món
            foreach (var item in order.OrderDetails)
            {
                if (item.Product != null && item.Product.DailyQuota.HasValue)
                {
                    item.Product.DailyQuota += item.Quantity;
                    item.Product.IsAvailable = true; // Bật lại vì vừa có hàng trả về
                }
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderChanged");
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderCreateDto dto)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // 1. Khôi phục lại Quota của các món trong hóa đơn CŨ
            foreach (var oldItem in order.OrderDetails)
            {
                if (oldItem.Product != null && oldItem.Product.DailyQuota.HasValue)
                {
                    oldItem.Product.DailyQuota += oldItem.Quantity;
                    oldItem.Product.IsAvailable = true;
                }
            }
            _context.OrderDetails.RemoveRange(order.OrderDetails);

            decimal totalAmount = 0;
            var newDetails = new List<OrderDetail>();

            // 2. Tính tiền và trừ lại Quota cho các món trong hóa đơn MỚI
            foreach (var item in dto.CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    if (product.DailyQuota.HasValue)
                    {
                        product.DailyQuota -= item.Quantity;
                        if (product.DailyQuota <= 0)
                        {
                            product.DailyQuota = 0;
                            product.IsAvailable = false;
                        }
                    }

                    newDetails.Add(new OrderDetail { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.Price });
                    totalAmount += (product.Price * item.Quantity);
                }
            }

            order.Note = dto.Note;
            order.Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : order.Status;
            order.OrderType = !string.IsNullOrEmpty(dto.OrderType) ? dto.OrderType : order.OrderType;
            order.TotalAmount = totalAmount;
            order.OrderDetails = newDetails;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("OrderChanged");
            return Ok(order);
        }

        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "Paid";
            await _context.SaveChangesAsync();
            return Ok();
        }

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

    // ==========================================
    // NẾU BẠN CHƯA CÓ DTO, HÃY DÁN ĐOẠN NÀY VÀO TRONG FILE HOẶC TẠO FILE MỚI
    // ==========================================
    public class OrderCreateDto
    {
        public string? Note { get; set; }
        public string? Status { get; set; }
        public string? OrderType { get; set; } // DineIn hoặc TakeAway
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}