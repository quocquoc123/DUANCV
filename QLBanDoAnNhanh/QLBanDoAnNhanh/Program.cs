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
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddHttpClient();
// Thêm dịch vụ SignalR cho chat
builder.Services.AddSignalR();

// Đăng ký Common class cho Dependency Injection
builder.Services.AddScoped<Common>();

// Cấu hình MVC
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

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

// Đảm bảo cột TrangThai trong bảng SanPham và Latitude, Longitude trong ChiNhanh tồn tại
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<QlbanDoAnNhanh3Context>();
    try
    {
        context.Database.ExecuteSqlRaw(
            "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SanPham') AND name = 'TrangThai') " +
            "ALTER TABLE SanPham ADD TrangThai BIT NOT NULL DEFAULT 1; " +
            "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChiNhanh') AND name = 'Latitude') " +
            "ALTER TABLE ChiNhanh ADD Latitude FLOAT NULL; " +
            "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChiNhanh') AND name = 'Longitude') " +
            "ALTER TABLE ChiNhanh ADD Longitude FLOAT NULL; " +
            "IF OBJECT_ID('Banner', 'U') IS NULL " +
            "CREATE TABLE Banner (" +
            "  MaBanner INT IDENTITY(1,1) NOT NULL PRIMARY KEY," +
            "  TieuDe NVARCHAR(200) NOT NULL," +
            "  HinhAnh NVARCHAR(500) NULL," +
            "  ViTri NVARCHAR(20) NOT NULL CONSTRAINT DF_Banner_ViTri DEFAULT N'Left'," +
            "  MaDm INT NULL," +
            "  ThuTu INT NOT NULL CONSTRAINT DF_Banner_ThuTu DEFAULT 0," +
            "  TrangThai BIT NOT NULL CONSTRAINT DF_Banner_TrangThai DEFAULT 1," +
            "  NgayCapNhat DATETIME NULL," +
            "  CONSTRAINT FK_Banner_DanhMuc FOREIGN KEY (MaDm) REFERENCES DanhMuc(maDM) ON DELETE SET NULL" +
            ");");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Lỗi cập nhật CSDL: " + ex.Message);
    }
}

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

