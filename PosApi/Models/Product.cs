using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PosApi.Models // Giữ nguyên namespace cũ của bạn
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty; // Giữ lại từ model cũ của bạn

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Tính năng kho tối giản: Gạt Còn/Hết món
        public bool IsAvailable { get; set; } = true;

        // Giới hạn số lượng bán trong ngày (ví dụ: Hôm nay chỉ có 20 suất bún đậu)
        public int? DailyQuota { get; set; }
    }
}