namespace PosApi.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Thời gian tạo đơn
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Tổng tiền của cả hóa đơn
        public decimal TotalAmount { get; set; }

        // Trạng thái đơn hàng: "New" (Mới), "Kitchen" (Đang làm), "Completed" (Hoàn thành)
        // Dùng cái này để khóa dữ liệu, chặn xóa món khi đang ở bếp
        public string Status { get; set; } = "New";

        // Ghi chú của khách hàng
        public string Note { get; set; } = string.Empty;

        // Một đơn hàng sẽ có nhiều chi tiết món ăn bên trong
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}