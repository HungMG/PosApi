using PosWebAdmin.Components;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// [QUAN TRỌNG NHẤT] ÉP DOCKER BẮT ĐÚNG SÓNG CỦA RENDER
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
// BẮT BUỘC TẮT DÒNG NÀY KHI LÊN RENDER DOCKER
// Lý do: Render đã tự bọc HTTPS bên ngoài. Nếu để dòng này, 
// bên trong app bị ép HTTPS 2 lần sẽ văng lỗi sập nguồn!
// ==========================================
// app.UseHttpsRedirection(); 

app.UseAntiforgery();

// app.MapStaticAssets();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();