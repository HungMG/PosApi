namespace PosApi.Models
{
    // Cái rổ hứng cục dữ liệu từ Mobile App gửi lên
    public class OrderCreateDto
    {
        public string Note { get; set; } = string.Empty;
        public List<CartItemDto> CartItems { get; set; } = new();
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}