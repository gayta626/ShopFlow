using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // Cung cấp [Authorize] để yêu cầu đăng nhập.
using System.Security.Claims; // Cung cấp tên claim chứa role.
using ShopFlow.Api.Dtos.Auth;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
        }

        return Ok(response);
    }

    [Authorize] // Chỉ cho phép truy cập khi JWT đã được xác thực thành công.
    [HttpGet("me")] // GET /api/auth/me; token gửi qua header, không cần body đăng nhập.
    public IActionResult Me() // Đọc claim có sẵn trong User nên không cần async.
    {
        var subject = User.FindFirst("sub")?.Value; // Lấy UserId từ claim sub của token.
        var role = User.FindFirst(ClaimTypes.Role)?.Value; // Lấy role từ token đã xác thực.

        if (!int.TryParse(subject, out var userId) || userId <= 0 || string.IsNullOrWhiteSpace(role)) // Kiểm tra danh tính có đủ dữ liệu hợp lệ.
        {
            return Unauthorized(); // Từ chối nếu thiếu hoặc sai định dạng claim cần thiết.
        }

        return Ok(new { userId, role }); // Trả 200 cùng danh tính trong token; không truy vấn database.
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]

    public IActionResult AdminInfo()
    {
        var subject = User.FindFirst("sub")?.Value; // Lấy UserId từ claim sub của token.
        var role = User.FindFirst(ClaimTypes.Role)?.Value; // Lấy role từ token đã xác thực.

        if (!int.TryParse(subject, out var userId) || userId <= 0 || string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(new { message = "Ban co quyen Admin" }); // 
    }
}
