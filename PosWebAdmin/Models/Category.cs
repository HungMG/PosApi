namespace PosWebAdmin.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Danh mục cha - con
        public int? ParentId { get; set; }
        public Category? Parent { get; set; }
        public List<Category> SubCategories { get; set; } = new();
    }
}