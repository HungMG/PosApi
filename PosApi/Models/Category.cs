using System.Collections.Generic;

namespace PosApi.Models // Sửa lại thành tên namespace dự án của bạn nếu cần
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // ==========================================
        // 1. CẤU TRÚC DANH MỤC CHA - CON
        // ==========================================
        public int? ParentId { get; set; }
        public Category? Parent { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // ==========================================
        // 2. LIÊN KẾT VỚI BẢNG MÓN ĂN (DÒNG BỊ THIẾU)
        // ==========================================
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}