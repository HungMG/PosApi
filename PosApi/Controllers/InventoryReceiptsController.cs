using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryReceiptsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryReceiptsController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách Lịch sử Nhập kho
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryReceipt>>> GetReceipts()
        {
            return await _context.InventoryReceipts
                .Include(r => r.Staff) // Lấy kèm tên nhân viên nhập
                .OrderByDescending(r => r.ImportDate)
                .ToListAsync();
        }

        // API NHẬP KHO CHÍNH THỨC
        [HttpPost]
        public async Task<ActionResult> CreateReceipt([FromBody] ReceiptCreateDto dto)
        {
            // Mở Transaction bảo vệ dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Khởi tạo Phiếu Nhập Kho (Mẹ)
                var receipt = new InventoryReceipt
                {
                    ImportDate = DateTime.UtcNow,
                    StaffId = dto.StaffId,
                    SupplierName = dto.SupplierName,
                    // Tự động tính tổng tiền trên Server cho an toàn (Lấy Số lượng x Giá nhập)
                    TotalCost = dto.Details.Sum(d => (decimal)d.Quantity * d.UnitPrice)
                };

                _context.InventoryReceipts.Add(receipt);
                await _context.SaveChangesAsync(); // Lưu để lấy ReceiptId

                // 2. Quét từng dòng chi tiết (Con) để lưu và cập nhật kho
                foreach (var item in dto.Details)
                {
                    var detail = new InventoryReceiptDetail
                    {
                        ReceiptId = receipt.Id,
                        IngredientId = item.IngredientId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    _context.InventoryReceiptDetails.Add(detail);

                    // 3. TÌM NGUYÊN LIỆU ĐỂ CỘNG KHO
                    var ingredient = await _context.Ingredients.FindAsync(item.IngredientId);
                    if (ingredient != null)
                    {
                        ingredient.CurrentStock += item.Quantity; // Cộng dồn số lượng
                        ingredient.CostPrice = item.UnitPrice;    // Cập nhật giá vốn mới nhất

                        // 4. AUTO-LOCK: Nếu món này có bán trực tiếp, tự động mở bán lại!
                        if (ingredient.IsLinkedToProduct && ingredient.LinkedProductId.HasValue)
                        {
                            var product = await _context.Products.FindAsync(ingredient.LinkedProductId.Value);
                            if (product != null)
                            {
                                product.IsAvailable = ingredient.CurrentStock > 0;
                            }
                        }
                    }
                }

                // Lưu tất cả và chốt Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(receipt);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Lỗi là hủy sạch, không để rác lại DB
                return StatusCode(500, "Lỗi khi nhập kho: " + ex.Message);
            }
        }

        // --- CÁI "RỔ" HỨNG DỮ LIỆU TỪ WEB GỬI LÊN ---
        public class ReceiptCreateDto
        {
            public int StaffId { get; set; }
            public string? SupplierName { get; set; }
            public List<ReceiptDetailDto> Details { get; set; } = new();
        }

        public class ReceiptDetailDto
        {
            public int IngredientId { get; set; }
            public double Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}