namespace PosApi.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // Trạng thái hiển thị danh mục (true: đang bán, false: tạm ẩn)
        public bool IsActive { get; set; } = true;

        // Mối quan hệ: 1 danh mục có thể có nhiều món ăn
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}