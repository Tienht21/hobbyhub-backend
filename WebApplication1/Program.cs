using HobbyManagementApi.Services;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình MongoDbSettings từ appsettings.json
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// 2. Đăng ký Services (Dependency Injection)
builder.Services.AddSingleton<HobbyService>();
builder.Services.AddSingleton<UserService>();

// 3. ĐĂNG KÝ CORS (Bắt buộc đặt TRƯỚC builder.Build())
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================================
// HOÀN TẤT ĐĂNG KÝ SERVICES -> BUILD APP (TỪ ĐÂY BẢNG SERVICES SẼ BỊ KHÓA)
// =========================================================================
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. KÍCH HOẠT CORS MIDDLEWARE (Bắt buộc nằm trước UseAuthorization và MapControllers)
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// 5. Seed tài khoản Admin mặc định
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    await userService.SeedAdminUserAsync();
}

app.Run();