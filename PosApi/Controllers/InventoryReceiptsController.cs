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

        // Xem lịch sử nhập hàng
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryReceipt>>> GetReceipts()
        {
            var receipts = await _context.InventoryReceipts
                .Include(r => r.ReceiptDetails)
                .ThenInclude(rd => rd.Ingredient) // Kéo theo tên nguyên liệu
                .Include(r => r.Staff) // Kéo theo tên người nhập
                .OrderByDescending(r => r.ImportDate)
                .ToListAsync();

            return Ok(receipts);
        }

        // TẠO PHIẾU NHẬP (Tự động cộng kho)
        [HttpPost]
        public async Task<ActionResult<InventoryReceipt>> PostReceipt(InventoryReceipt receipt)
        {
            if (receipt.ReceiptDetails == null || !receipt.ReceiptDetails.Any())
                return BadRequest("Phiếu nhập không có mặt hàng nào!");

            receipt.ImportDate = DateTime.UtcNow;

            // 1. Lưu phiếu nhập vào DB
            _context.InventoryReceipts.Add(receipt);

            // 2. Chạy vòng lặp để CỘNG số lượng vào kho thực tế
            foreach (var detail in receipt.ReceiptDetails)
            {
                var ingredient = await _context.Ingredients.FindAsync(detail.IngredientId);
                if (ingredient != null)
                {
                    ingredient.CurrentStock += detail.Quantity; // Cộng dồn tồn kho
                }
            }

            await _context.SaveChangesAsync();
            return Ok(receipt);
        }
    }
}