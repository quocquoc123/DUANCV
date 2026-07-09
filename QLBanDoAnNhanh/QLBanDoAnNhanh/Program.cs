using QLBanDoAnNhanh.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using QLBanDoAnNhanh.Hubs;
using QLBanDoAnNhanh.Common;
using DinkToPdf.Contracts;
using DinkToPdf;
using QLBanDoAnNhanh.Services;
using QLBanDoAnNhanh.Repositories;




var builder = WebApplication.CreateBuilder(args);

// Cấu hình dịch vụ
builder.Services.AddHttpClient();
builder.Services.AddDbContext<QlbanDoAnNhanh3Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("QLBanDoAnNhanh"));
});

builder.Services.Configure<MomoSettings>(builder.Configuration.GetSection("MoMo"));
builder.Services.AddHttpClient<MomoService>();

builder.Services.AddScoped<PayPalService>();
builder.Services.AddScoped<VoucherService>();
builder.Services.AddScoped<IProductDiscountRepository, ProductDiscountRepository>();
builder.Services.AddScoped<IProductDiscountService, ProductDiscountService>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddHttpClient();
// Thêm dịch vụ SignalR cho chat
builder.Services.AddSignalR();

// Đăng ký Common class cho Dependency Injection
builder.Services.AddScoped<Common>();

// Cấu hình MVC
builder.Services.AddControllersWithViews();

// Cấu hình dịch vụ lưu trữ session
builder.Services.AddDistributedMemoryCache(); // Lưu session vào bộ nhớ tạm
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian timeout cho session
    options.Cookie.HttpOnly = true; // Cookie chỉ truy cập qua HTTP
    options.Cookie.IsEssential = true; // Cho phép session luôn hoạt động
});

// Thêm dịch vụ PDF converter
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

var app = builder.Build();

// Cấu hình pipeline xử lý request
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseSession(); // Kích hoạt session
app.UseRouting();
app.UseAuthorization();

// Cấu hình routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=SanPhams}/{action=TrangChu}/{id?}");
app.MapControllers();

// Định tuyến SignalR cho chat
app.MapHub<ChatHub>("/chatHub");

app.Run();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");


// Sử dụng tiếng Việt
var supportedCultures = new[] { "vi-VN", "en-US" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

