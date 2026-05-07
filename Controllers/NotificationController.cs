using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Models;
using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<NotificationDto>>> FindAll([FromQuery] NotificationQueryDto queryDto)
        {
            var result = await _notificationService.FindAll(CurrentUserId, queryDto);
            return Ok(result);
        }

        [HttpPatch("{id}/seen")]
        public async Task<ActionResult<NotificationDto>> UpdateSeen(int id, [FromBody] UpdateSeenDto updateSeenDto)
        {
            var result = await _notificationService.UpdateSeen(id, CurrentUserId, updateSeenDto.Seen);
            return Ok(result);
        }
    }
}
