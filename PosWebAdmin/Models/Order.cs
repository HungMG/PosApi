namespace PosWebAdmin.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "New";
        public string Note { get; set; } = string.Empty;
        public List<OrderDetail> OrderDetails { get; set; } = new();
    }
}