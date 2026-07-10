using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PosApi.Models
{
    public class ShiftReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StaffId { get; set; }

        [ForeignKey("StaffId")]
        public virtual Staff? Staff { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.UtcNow;

        // ==========================================
        // 1. VÙNG TIỀN MẶT (Nhân viên phải đếm)
        // ==========================================
        [Column(TypeName = "decimal(18,2)")]
        public decimal StartingCash { get; set; } = 0; // Tiền lẻ sếp giao đầu ca

        [Column(TypeName = "decimal(18,2)")]
        public decimal SystemCashAmount { get; set; } = 0; // Tổng bill Tiền Mặt (App tính)

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualCashAmount { get; set; } = 0; // Tiền nhân viên đếm thực tế trong két

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashDifference { get; set; } = 0; // Lệch két: ActualCash - (Starting + SystemCash)

        // ==========================================
        // 2. VÙNG CHUYỂN KHOẢN (Báo cáo cho Sếp)
        // ==========================================
        [Column(TypeName = "decimal(18,2)")]
        public decimal SystemTransferAmount { get; set; } = 0; // Tổng bill Chuyển Khoản (App tính)

        [MaxLength(255)]
        public string Note { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Pending";
    }
}