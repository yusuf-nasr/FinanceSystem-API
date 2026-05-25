using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v0/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        // GET /api/v1/notifications
        [HttpGet]
        public async Task<ActionResult> FindAll([FromQuery] NotificationQueryDTO query)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.FindAllAsync(userId, query);
            return Ok(result);
        }

        // PATCH /api/v1/notifications/:id/seen
        [HttpPatch("{id}/seen")]
        public async Task<ActionResult<NotificationDTO>> UpdateSeen(int id, [FromBody] UpdateNotificationSeenDTO dto)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.UpdateSeenAsync(id, userId, dto.Seen);
            return Ok(result);
        }
    }
}
