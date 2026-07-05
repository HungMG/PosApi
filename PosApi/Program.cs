using Microsoft.EntityFrameworkCore;
using PosApi.Data;
using PosApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CẤU HÌNH CORS (CHO PHÉP WEB GỌI API)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// ==========================================
// 2. CẤU HÌNH DATABASE NEON.TECH
// ==========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// 3. CẤU HÌNH CHỐNG LỖI VÒNG LẶP JSON 500
// ==========================================
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddSignalR();

// TẠM TẮT DÒNG NÀY ĐỂ TRÁNH LỖI BUILD:
// builder.Services.AddOpenApi();

var app = builder.Build();

// ==========================================
// 4. KÍCH HOẠT CORS (Phải đặt TRƯỚC MapControllers)
// ==========================================
app.UseCors("AllowAll");

// TẠM TẮT KHỐI NÀY ĐỂ TRÁNH LỖI BUILD:
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }
app.MapHub<OrderHub>("/orderHub");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();