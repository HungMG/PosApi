using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PosApi.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled

        [MaxLength(500)]
        public string? Note { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderType { get; set; } = "Tại chỗ"; // Sửa lại thành tiếng Việt cho khớp bộ lọc

        // 👉 DÒNG MỚI ĐƯỢC THÊM VÀO:
        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Sẽ lưu "Tiền mặt" hoặc "Chuyển khoản"

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}