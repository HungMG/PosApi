using System.ComponentModel.DataAnnotations;

namespace PosApi.Models
{
    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty; // kg, chai, hop...

        public double CurrentStock { get; set; } = 0;

        public double MinStock { get; set; } = 0; // Ngưỡng báo động hết hàng

        // --- CÁC BIẾN MỚI THÊM VÀO ---
        public decimal CostPrice { get; set; } = 0; // Giá vốn nhập kho
        public bool IsLinkedToProduct { get; set; } = false; // Cờ xác nhận có bán trực tiếp không
        public int? LinkedProductId { get; set; } // ID của món ăn/thức uống tương ứng bên bảng Products
    }
}