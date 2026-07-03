namespace PosApi.DTOs
{
    // Đây là cấu trúc dữ liệu mà Mobile App sẽ "đóng gói" để gửi lên Server
    public class OrderRequestDto
    {
        public string Note { get; set; } = string.Empty;

        // Danh sách các món trong giỏ hàng
        public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}