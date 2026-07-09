using System.ComponentModel.DataAnnotations;

namespace PosApi.Models
{
    public class Stocktake
    {
        [Key]
        public int Id { get; set; }
        public DateTime CheckDate { get; set; } = DateTime.UtcNow; // Ngày giờ đi kiểm kho

        public virtual ICollection<StocktakeDetail> Details { get; set; } = new List<StocktakeDetail>();
    }
}