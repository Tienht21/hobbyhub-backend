using HobbyManagementApi.Models;
using HobbyManagementApi.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace HobbyManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HobbyController : ControllerBase
    {
        private readonly HobbyService _hobbyService;

        public HobbyController(HobbyService hobbyService)
        {
            _hobbyService = hobbyService;
        }

        // GET: api/Hobby?userId=usr_101&role=User&type=READING
        [HttpGet]
        public async Task<ActionResult<List<HobbyItem>>> Get(
            [FromQuery] string userId = "usr_101",
            [FromQuery] string role = "User",
            [FromQuery] string? type = null)
        {
            var hobbies = await _hobbyService.GetAsync(userId, role, type);
            return Ok(hobbies);
        }

        // GET: api/Hobby/660a1001abcdef0000000001
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<HobbyItem>> GetById(string id)
        {
            var hobby = await _hobbyService.GetByIdAsync(id);
            if (hobby is null) return NotFound(new { message = "Không tìm thấy sở thích này!" });
            return Ok(hobby);
        }

        // POST: api/Hobby
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] HobbyItem newHobby)
        {
            if (string.IsNullOrEmpty(newHobby.Title) || string.IsNullOrEmpty(newHobby.UserId))
            {
                return BadRequest(new { message = "Tiêu đề và Mã người dùng không được để trống!" });
            }

            await _hobbyService.CreateAsync(newHobby);
            return CreatedAtAction(nameof(GetById), new { id = newHobby.Id }, newHobby);
        }

        // PUT: api/Hobby/660a1001abcdef0000000001?currentUserId=usr_101&role=User
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] HobbyItem updatedHobby,
            [FromQuery] string currentUserId = "usr_101",
            [FromQuery] string role = "User")
        {
            var success = await _hobbyService.UpdateAsync(id, updatedHobby, currentUserId, role);
            if (!success)
            {
                return StatusCode(403, new { message = "Bạn không có quyền sửa sở thích của người khác hoặc không tìm thấy dữ liệu!" });
            }

            return NoContent();
        }

        // DELETE: api/Hobby/660a1001abcdef0000000001?currentUserId=usr_101&role=User
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(
            string id,
            [FromQuery] string currentUserId = "usr_101",
            [FromQuery] string role = "User")
        {
            var success = await _hobbyService.RemoveAsync(id, currentUserId, role);
            if (!success)
            {
                return StatusCode(403, new { message = "Bạn không có quyền xóa sở thích của người khác hoặc không tìm thấy dữ liệu!" });
            }

            return Ok(new { message = "Xóa thành công khỏi MongoDB!" });
        }

        // GET: api/Hobby/stats?userId=usr_101&role=User
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(
            [FromQuery] string userId = "usr_101",
            [FromQuery] string role = "User")
        {
            var stats = await _hobbyService.GetStatisticsAsync(userId, role);
            var jsonResult = stats.Select(doc => doc.ToJson()).ToList();
            return Ok(jsonResult);
        }
    }
}