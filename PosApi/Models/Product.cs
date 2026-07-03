namespace PosApi.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // Giá tiền của món ăn
        public decimal Price { get; set; }

        // Đường dẫn hình ảnh lưu trên cloud/web
        public string ImageUrl { get; set; } = string.Empty;

        // Trạng thái kho hàng (true: còn hàng, false: hết hàng - dùng để khóa món trên App order)
        public bool IsAvailable { get; set; } = true;

        // Khóa ngoại liên kết tới bảng Danh mục
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}