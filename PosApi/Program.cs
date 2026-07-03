using Microsoft.EntityFrameworkCore;
using PosApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình kết nối Database PostgreSQL (Neon.tech)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// TẠM TẮT DÒNG NÀY ĐỂ TRÁNH LỖI BUILD:
// builder.Services.AddOpenApi();

var app = builder.Build();

// TẠM TẮT KHỐI NÀY ĐỂ TRÁNH LỖI BUILD:
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();