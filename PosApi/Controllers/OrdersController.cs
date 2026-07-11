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

        // ==========================================
        // 1. TẠO ĐƠN HÀNG (TRỪ QUOTA + TRỪ KHO + SIGNALR)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (dto.CartItems == null || !dto.CartItems.Any())
                return BadRequest("Giỏ hàng đang trống!");

            // Bật Transaction bảo vệ dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal totalAmount = 0;
                var orderDetails = new List<OrderDetail>();

                foreach (var item in dto.CartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        if (!product.IsAvailable)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest($"Món {product.Name} đã hết hàng!");
                        }

                        // A. XỬ LÝ QUOTA TRONG NGÀY
                        if (product.DailyQuota.HasValue)
                        {
                            if (product.DailyQuota.Value < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest($"Món {product.Name} chỉ còn {product.DailyQuota.Value} phần!");
                            }

                            product.DailyQuota -= item.Quantity;
                            if (product.DailyQuota <= 0)
                            {
                                product.DailyQuota = 0;
                                product.IsAvailable = false;
                            }
                        }

                        // B. XỬ LÝ TRỪ KHO ĐÓNG CHAI (Đã thêm lệnh chặn kho âm)
                        var linkedIngredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.LinkedProductId == item.ProductId);
                        if (linkedIngredient != null)
                        {
                            // 👉 CHỐT CHẶN MỚI: Kiểm tra kho thực tế trước khi trừ
                            if (linkedIngredient.CurrentStock < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest($"Trong kho chỉ còn {linkedIngredient.CurrentStock} phần {product.Name}!");
                            }

                            linkedIngredient.CurrentStock -= item.Quantity;
                            if (linkedIngredient.CurrentStock <= 0)
                            {
                                linkedIngredient.CurrentStock = 0;
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
                    OrderType = !string.IsNullOrEmpty(dto.OrderType) ? dto.OrderType : "Tại chỗ",
                    PaymentMethod = dto.PaymentMethod,
                    OrderDetails = orderDetails
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("OrderChanged");
                return Ok(order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi tạo đơn: " + ex.Message);
            }
        }
        // ==========================================
        // 2. HỦY ĐƠN HÀNG (HOÀN TRẢ QUOTA + HOÀN KHO)
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Hoàn trả lại Quota và Tồn kho
                foreach (var item in order.OrderDetails)
                {
                    // Trả Quota
                    if (item.Product != null && item.Product.DailyQuota.HasValue)
                    {
                        item.Product.DailyQuota += item.Quantity;
                        item.Product.IsAvailable = true;
                    }

                    // Trả Tồn Kho (Sting, Nước suối...)
                    var linkedIngredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.LinkedProductId == item.ProductId);
                    if (linkedIngredient != null)
                    {
                        linkedIngredient.CurrentStock += item.Quantity;
                        if (item.Product != null) item.Product.IsAvailable = true;
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("OrderChanged");
                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi hủy đơn: " + ex.Message);
            }
        }

        // ==========================================
        // 3. CẬP NHẬT ĐƠN HÀNG
        // ==========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderCreateDto dto)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // BƯỚC 1: KHÔI PHỤC LẠI QUOTA VÀ KHO TỪ ĐƠN CŨ
                foreach (var oldItem in order.OrderDetails)
                {
                    if (oldItem.Product != null && oldItem.Product.DailyQuota.HasValue)
                    {
                        oldItem.Product.DailyQuota += oldItem.Quantity;
                        oldItem.Product.IsAvailable = true;
                    }

                    var oldLinkedIng = await _context.Ingredients.FirstOrDefaultAsync(i => i.LinkedProductId == oldItem.ProductId);
                    if (oldLinkedIng != null)
                    {
                        oldLinkedIng.CurrentStock += oldItem.Quantity;
                        if (oldItem.Product != null) oldItem.Product.IsAvailable = true;
                    }
                }

                _context.OrderDetails.RemoveRange(order.OrderDetails);

                decimal totalAmount = 0;
                var newDetails = new List<OrderDetail>();

                // BƯỚC 2: TÍNH TIỀN VÀ TRỪ LẠI QUOTA + KHO CHO HÓA ĐƠN MỚI
                foreach (var item in dto.CartItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Trừ Quota
                        if (product.DailyQuota.HasValue)
                        {
                            // 👉 CHỐT CHẶN MỚI: Bắt buộc kiểm tra lại sau khi đã khôi phục kho cũ
                            if (product.DailyQuota.Value < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest($"Cập nhật thất bại: {product.Name} chỉ còn tối đa {product.DailyQuota.Value} phần!");
                            }

                            product.DailyQuota -= item.Quantity;
                            if (product.DailyQuota <= 0)
                            {
                                product.DailyQuota = 0;
                                product.IsAvailable = false;
                            }
                        }

                        // Trừ Tồn Kho
                        var newLinkedIng = await _context.Ingredients.FirstOrDefaultAsync(i => i.LinkedProductId == item.ProductId);
                        if (newLinkedIng != null)
                        {
                            // 👉 CHỐT CHẶN MỚI: Tránh âm kho khi Sửa đơn
                            if (newLinkedIng.CurrentStock < item.Quantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest($"Cập nhật thất bại: Kho chỉ còn {newLinkedIng.CurrentStock} phần {product.Name}!");
                            }

                            newLinkedIng.CurrentStock -= item.Quantity;
                            if (newLinkedIng.CurrentStock <= 0)
                            {
                                newLinkedIng.CurrentStock = 0;
                                product.IsAvailable = false;
                            }
                        }

                        newDetails.Add(new OrderDetail { ProductId = product.Id, Quantity = item.Quantity, UnitPrice = product.Price });
                        totalAmount += (product.Price * item.Quantity);
                    }
                }

                // BƯỚC 3: CẬP NHẬT THÔNG TIN BILL
                order.Note = dto.Note;
                order.Status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : order.Status;
                order.OrderType = !string.IsNullOrEmpty(dto.OrderType) ? dto.OrderType : order.OrderType;
                order.PaymentMethod = !string.IsNullOrEmpty(dto.PaymentMethod) ? dto.PaymentMethod : order.PaymentMethod;
                order.TotalAmount = totalAmount;
                order.OrderDetails = newDetails;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("OrderChanged");
                return Ok(order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Lỗi cập nhật đơn: " + ex.Message);
            }
        }

        // ==========================================
        // 4. CHUYỂN TRẠNG THÁI THANH TOÁN
        // ==========================================
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = "Paid";
            await _context.SaveChangesAsync();

            // 👉 THÊM DÒNG NÀY ĐỂ BÁO CHO WEB ADMIN VÀ CÁC APP KHÁC TẢI LẠI TRANG
            await _hubContext.Clients.All.SendAsync("OrderChanged");
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
    // CÁC LỚP DTO
    // ==========================================
    public class OrderCreateDto
    {
        public string? Note { get; set; }
        public string? Status { get; set; }
        public string? OrderType { get; set; } // DineIn hoặc TakeAway
        public string? PaymentMethod { get; set; } // Tiền mặt / Chuyển khoản
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}