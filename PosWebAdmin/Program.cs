using PosWebAdmin.Components;
using System; // Bắt buộc phải có để xài Exception

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ==========================================
    // KHẮC PHỤC LỖI PORT RỖNG TRÊN RENDER
    // ==========================================
    var port = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrWhiteSpace(port))
    {
        port = "8080";
    }
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Đăng ký ApiService vào hệ thống
    builder.Services.AddHttpClient<PosWebAdmin.Services.ApiService>();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

    app.UseAntiforgery();

    app.UseStaticFiles();
    app.UseRouting();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    // ==========================================
    // ÉP BUỘC PHẢI KHAI BÁO NẾU SẬP NGUỒN
    // ==========================================
    Console.WriteLine("==================================================");
    Console.WriteLine("CẢNH BÁO ĐỎ: ỨNG DỤNG SẬP NGAY LÚC KHỞI ĐỘNG!");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("==================================================");
}