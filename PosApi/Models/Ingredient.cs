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
    }
}