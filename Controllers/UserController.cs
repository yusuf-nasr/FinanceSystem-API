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
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        private Role GetCurrentUserRole()
        {
            var roleStr = User.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<Role>(roleStr, out var role) ? role : Role.USER;
        }

        // POST /api/v1/users — Admin only
        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> CreateUser([FromBody] UserCreateDTO request)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _userService.CreateUserAsync(request);
            return StatusCode(201, result);
        }

        // GET /api/v1/users
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<UserResponseDTO>>> GetUsers([FromQuery] UserQueryDTO query)
        {
            var role = GetCurrentUserRole();

            // Non-admins cannot filter by active status
            if (role != Role.ADMIN && query.Active.HasValue)
                throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                    new Dictionary<string, object> { { "fields", "active" } });

            var result = await _userService.FindAllAsync(query);
            return Ok(result);
        }

        // GET /api/v1/users/me
        [HttpGet("me")]
        public async Task<ActionResult<UserResponseDTO>> GetMe()
        {
            var userId = GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                throw new ApiException(404, ErrorCode.USER_NOT_FOUND,
                    new Dictionary<string, object> { { "id", userId } });
            return Ok(user);
        }

        // GET /api/v1/users/:id
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDTO>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                throw new ApiException(404, ErrorCode.USER_NOT_FOUND,
                    new Dictionary<string, object> { { "id", id } });
            return Ok(user);
        }

        // PATCH /api/v1/users/:id
        [HttpPatch("{id}")]
        public async Task<ActionResult<UserResponseDTO>> UpdateUser(int id, [FromBody] UserUpdateDTO request)
        {
            var uid = GetCurrentUserId();
            var isAdmin = GetCurrentUserRole() == Role.ADMIN;

            // Only admin can update other users; a user can only update themselves
            if (!isAdmin && uid != id)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            // Non-admins can only update 'name' and 'password'
            if (!isAdmin)
            {
                var forbiddenFields = new List<string>();
                if (request.role.HasValue) forbiddenFields.Add("role");
                if (request.Active.HasValue) forbiddenFields.Add("active");
                if (request.DepartmentName != null) forbiddenFields.Add("departmentName");

                if (forbiddenFields.Count > 0)
                    throw new ApiException(403, ErrorCode.RESTRICTED_FIELD_UPDATE,
                        new Dictionary<string, object> { { "fields", string.Join(", ", forbiddenFields) } });
            }

            var result = await _userService.UpdateUserAsync(id, request);
            return Ok(result);
        }

        // DELETE /api/v1/users/:id — Admin only
        [HttpDelete("{id}")]
        public async Task<ActionResult<UserResponseDTO>> DeleteUser(int id)
        {
            if (GetCurrentUserRole() != Role.ADMIN)
                throw new ApiException(403, ErrorCode.MISSING_ROLE);

            var result = await _userService.DeleteUserAsync(id);
            return Ok(result);
        }
    }
}