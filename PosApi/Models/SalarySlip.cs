namespace PosApi.Models // Đổi thành PosWebAdminWasm.Models khi dán vào Web
{
    public class SalarySlip
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public double TotalHours { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal BaseSalary { get; set; }

        public decimal Bonus { get; set; }
        public decimal Penalty { get; set; }
        public decimal FinalSalary { get; set; }

        public DateTime PaymentDate { get; set; }
    }
}