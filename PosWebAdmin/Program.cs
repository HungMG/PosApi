using System; // Đảm bảo không bị lỗi biến Environment
using PosWebAdmin.Components;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ÉP DOCKER BẮT ĐÚNG CỔNG CỦA RENDER
// ==========================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đăng ký ApiService vào hệ thống
builder.Services.AddHttpClient<PosWebAdmin.Services.ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// ==========================================
// 2. TẮT CHUYỂN HƯỚNG HTTPS (Chống sập do vòng lặp)
// ==========================================
// app.UseHttpsRedirection(); 

app.UseAntiforgery();

// Trả lại hàm gốc của bạn để tải mượt nhất
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();