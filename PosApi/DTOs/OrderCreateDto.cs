namespace PosApi.Models // Hoặc PosApi.DTOs tùy bạn sắp xếp thư mục
{
    // Cái rổ hứng cục dữ liệu từ Mobile App và Web Admin gửi lên
    public class OrderCreateDto
    {
        public string Note { get; set; } = string.Empty;

        // Hứng trạng thái (New, Pending, Kitchen, Paid...)
        public string Status { get; set; } = "New";

        // Hứng loại đơn hàng (DineIn: Tại chỗ, TakeAway: Mang đi)
        public string OrderType { get; set; } = "DineIn";

        // Danh sách các món trong giỏ hàng
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}