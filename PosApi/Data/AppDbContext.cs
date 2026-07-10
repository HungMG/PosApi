using Microsoft.EntityFrameworkCore;
using PosApi.Models;

namespace PosApi.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Khai báo 10 bảng của Trạm 1
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Salary> Salaries { get; set; }

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<InventoryReceipt> InventoryReceipts { get; set; }
        public DbSet<InventoryReceiptDetail> InventoryReceiptDetails { get; set; }
        public DbSet<SalarySlip> SalarySlips { get; set; }

        public DbSet<Stocktake> Stocktakes { get; set; }
        public DbSet<StocktakeDetail> StocktakeDetails { get; set; }

        public DbSet<ShiftReport> ShiftReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình để khi xóa Category cha thì KHÔNG tự động xóa Category con (tránh lỗi xung đột khóa ngoại)
            modelBuilder.Entity<Category>()
                .HasMany(c => c.SubCategories) // Theo model cũ của bạn là SubCategories
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}