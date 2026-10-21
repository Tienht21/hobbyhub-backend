using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class UserService
    {
        private readonly IMongoCollection<UserAccount> _userCollection;

        public UserService(IOptions<MongoDbSettings> settings)
        {
            var mongoClient = new MongoClient(settings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<UserAccount>("users");
        }

        // 1. Đăng ký tài khoản
        // 1. Đăng ký tài khoản (Luôn cố định Role = User)
        public async Task<(bool success, string message)> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userCollection.Find(u => u.Username == dto.Username || u.Email == dto.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return (false, "Tên đăng nhập hoặc Email đã tồn tại!");
            }

            var newUser = new UserAccount
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = HashPassword(dto.Password),
                Role = "User" // KHÓA CỐ ĐỊNH: Mọi tài khoản mới luôn là User
            };

            await _userCollection.InsertOneAsync(newUser);
            return (true, "Đăng ký tài khoản thành công!");
        }

        // 2. Đăng nhập
        public async Task<(bool success, string message, UserAccount? user)> LoginAsync(LoginDto dto)
        {
            var hashedPassword = HashPassword(dto.Password);
            var user = await _userCollection.Find(u =>
                (u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail) &&
                u.PasswordHash == hashedPassword).FirstOrDefaultAsync();

            if (user == null)
            {
                return (false, "Mật khẩu hoặc tài khoản không chính xác!", null);
            }

            return (true, "Đăng nhập thành công!", user);
        }

        // 3. Yêu cầu Quên mật khẩu (Tạo mã OTP 6 chữ số)
        public async Task<(bool success, string message, string? otpCode)> ForgotPasswordAsync(string email)
        {
            var user = await _userCollection.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null)
            {
                return (false, "Email này chưa được đăng ký!", null);
            }

            // Tạo OTP ngẫu nhiên 6 chữ số
            string otp = new Random().Next(100000, 999999).ToString();
            user.ResetOtp = otp;
            user.ResetOtpExpires = DateTime.UtcNow.AddMinutes(15); // Hạn 15 phút

            await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
            return (true, "Mã xác thực OTP đã được khởi tạo!", otp);
        }

        // 4. Đặt lại Mật khẩu bằng OTP
        public async Task<(bool success, string message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userCollection.Find(u => u.Email == dto.Email).FirstOrDefaultAsync();
            if (user == null) return (false, "Email không tồn tại!");

            if (user.ResetOtp != dto.OtpCode || user.ResetOtpExpires < DateTime.UtcNow)
            {
                return (false, "Mã OTP không đúng hoặc đã hết hạn!");
            }

            user.PasswordHash = HashPassword(dto.NewPassword);
            user.ResetOtp = null;
            user.ResetOtpExpires = null;

            await _userCollection.ReplaceOneAsync(u => u.Id == user.Id, user);
            return (true, "Đổi mật khẩu thành công! Bạn có thể đăng nhập ngay.");
        }

        // Hàm mã hóa SHA256 mật khẩu an toàn
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        // Hàm tự động tạo 1 tài khoản Admin duy nhất khi ứng dụng khởi chạy
        public async Task SeedAdminUserAsync()
        {
            var adminExists = await _userCollection.Find(u => u.Role == "Admin").AnyAsync();
            if (!adminExists)
            {
                var adminUser = new UserAccount
                {
                    Username = "admin",
                    Email = "tienadmin",
                    PasswordHash = HashPassword("admin123"), // Mật khẩu mặc định: admin123
                    Role = "Admin"
                };
                await _userCollection.InsertOneAsync(adminUser);
            }
        }

    }
}