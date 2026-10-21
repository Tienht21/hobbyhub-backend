using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _userService.RegisterAsync(dto);
            if (!result.success) return BadRequest(new { message = result.message });
            return Ok(new { message = result.message });
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);
            if (!result.success) return Unauthorized(new { message = result.message });

            return Ok(new
            {
                message = result.message,
                userId = result.user!.Username,
                email = result.user.Email,
                role = result.user.Role
            });
        }

        // POST: api/Auth/google-login
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            try
            {
                // Giải mã và xác thực Token gửi từ Google SDK ở Frontend
                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

                var email = payload.Email;
                var username = email.Split('@')[0]; // Tự động tạo Username từ Email

                return Ok(new
                {
                    message = "Đăng nhập bằng Google thành công!",
                    userId = username,
                    email = email,
                    role = "User"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Xác thực Token Google thất bại: " + ex.Message });
            }
        }

        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _userService.ForgotPasswordAsync(dto.Email);
            if (!result.success) return BadRequest(new { message = result.message });

            return Ok(new
            {
                message = result.message,
                otpCodeDemo = result.otpCode // Trả về OTP để test demo dễ dàng
            });
        }

        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _userService.ResetPasswordAsync(dto);
            if (!result.success) return BadRequest(new { message = result.message });

            return Ok(new { message = result.message });
        }
    }
}