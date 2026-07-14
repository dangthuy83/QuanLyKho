using KhoQuanLy.Repositories;
using KhoQuanLy.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddScoped<IDbConnectionFactory>(_ => new MySqlConnectionFactory(connStr));

// Repositories
builder.Services.AddScoped<INhomVatTuRepository, NhomVatTuRepository>();
builder.Services.AddScoped<IVatTuRepository, VatTuRepository>();
builder.Services.AddScoped<INhaCungCapRepository, NhaCungCapRepository>();
builder.Services.AddScoped<IBoPhanRepository, BoPhanRepository>();
builder.Services.AddScoped<IPhieuSPRepository, PhieuSPRepository>();
builder.Services.AddScoped<IDeNghiXuatRepository, DeNghiXuatRepository>();
builder.Services.AddScoped<IPhieuXKTCRepository, PhieuXKTCRepository>();
builder.Services.AddScoped<ITraLaiKhoRepository, TraLaiKhoRepository>();
builder.Services.AddScoped<IGiayTietKiemRepository, GiayTietKiemRepository>();
builder.Services.AddScoped<IKhuonBeRepository, KhuonBeRepository>();
builder.Services.AddScoped<IDinhMucRepository, DinhMucRepository>();
builder.Services.AddScoped<ITonKhoRepository, TonKhoRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IDanhMucService, DanhMucService>();
builder.Services.AddScoped<IPhieuSPService, PhieuSPService>();
builder.Services.AddScoped<IDeNghiXuatService, DeNghiXuatService>();
builder.Services.AddScoped<IPhieuXKTCService, PhieuXKTCService>();
builder.Services.AddScoped<ITraLaiKhoService, TraLaiKhoService>();
builder.Services.AddScoped<IGiayTietKiemService, GiayTietKiemService>();
builder.Services.AddScoped<IKhuonBeService, KhuonBeService>();
builder.Services.AddScoped<IDinhMucService, DinhMucService>();
builder.Services.AddScoped<ITonKhoService, TonKhoService>();
builder.Services.AddScoped<IBaoCaoService, BaoCaoService>();
builder.Services.AddScoped<IPdfParserService, PdfParserService>();
builder.Services.AddScoped<IExcelParserService, ExcelParserService>();
builder.Services.AddScoped<IPdfStorageService, PdfStorageService>();
builder.Services.AddScoped<IConfigSettingsService, ConfigSettingsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
