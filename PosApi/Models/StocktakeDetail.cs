using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PosApi.Models
{
    public class StocktakeDetail
    {
        [Key]
        public int Id { get; set; }

        public int StocktakeId { get; set; }
        [JsonIgnore]
        public virtual Stocktake? Stocktake { get; set; }

        public int IngredientId { get; set; }
        public virtual Ingredient? Ingredient { get; set; }

        public double SystemStock { get; set; } // Số cũ của phần mềm
        public double ActualStock { get; set; } // Số thực tế nhân viên đếm
        public double Discrepancy { get; set; } // Độ lệch (Thực tế - Phần mềm)
    }
}