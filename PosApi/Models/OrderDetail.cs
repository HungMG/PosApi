namespace PosApi.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        // Số lượng món khách đặt
        public int Quantity { get; set; }

        // Giá tại thời điểm đặt (lưu lại để lỡ sau này giá Menu đổi thì hóa đơn cũ không bị lệch)
        public decimal UnitPrice { get; set; }

        // Khóa ngoại liên kết với Đơn hàng tổng
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        // Khóa ngoại liên kết với Món ăn
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}