using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PosApi.Models
{
    public class InventoryReceipt
    {
        [Key]
        public int Id { get; set; }

        public DateTime ImportDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [MaxLength(200)]
        public string? SupplierName { get; set; }

        [Required]
        public int StaffId { get; set; } // Người thực hiện nhập kho

        [ForeignKey("StaffId")]
        public virtual Staff? Staff { get; set; }

        public virtual ICollection<InventoryReceiptDetail> ReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();
    }
}