using System.ComponentModel.DataAnnotations;

namespace PosApi.Models
{
    public class Staff
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Staff"; // Admin, Cashier, Kitchen, Staff

        public bool IsActive { get; set; } = true;
    }
}