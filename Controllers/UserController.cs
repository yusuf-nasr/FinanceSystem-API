using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using FinanceSystem_Dotnet.Exceptions;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFinanceService _financeService;

        public UserController(IUserService userService, IFinanceService financeService)
        {
            _userService = userService;
            _financeService = financeService;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateUser([FromBody] UserCreateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!_financeService.IsAdmin(UID))
            {
                return Forbid();
            }

            var result = await _userService.CreateUserAsync(request);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            var paginated = await _userService.GetAllUsersPaginatedAsync(page, perPage);
            return Ok(paginated);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult> SearchUsers([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int perPage = 10)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Name query parameter is required" });

            var result = await _userService.SearchUsersByNameAsync(name, page, perPage);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }

        [HttpPatch("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserUpdateDTO request)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var isAdmin = _financeService.IsAdmin(UID);
            if (!isAdmin && UID != id)
            {
                return Forbid();
            }

            // Non-admin users can only update 'name' and 'password'
            if (!isAdmin)
            {
                var forbiddenFields = new List<string>();
                if (request.role != default) forbiddenFields.Add("role");
                if (request.Active != default) forbiddenFields.Add("active");
                if (!string.IsNullOrEmpty(request.DepartmentName)) forbiddenFields.Add("departmentName");

                if (forbiddenFields.Count > 0)
                {
                    throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                        new Dictionary<string, object> { { "fields", string.Join(", ", forbiddenFields) } });
                }
            }

            var result = await _userService.UpdateUserAsync(id, request, isAdmin);
            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteUser(int id)
        {
            int UID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!_financeService.IsAdmin(UID))
            {
                return Forbid();
            }

            var result = await _userService.DeleteUserAsync(id);
            if (!result.Success)
            {
                return NotFound(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
    }
}