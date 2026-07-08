using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PosApi.Models
{
    public class InventoryReceiptDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReceiptId { get; set; }

        [ForeignKey("ReceiptId")]
        [JsonIgnore]
        public virtual InventoryReceipt? Receipt { get; set; }

        [Required]
        public int IngredientId { get; set; }

        [ForeignKey("IngredientId")]
        public virtual Ingredient? Ingredient { get; set; }

        [Required]
        public double Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}