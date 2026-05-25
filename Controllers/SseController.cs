using FinanceSystem_Dotnet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v0/sse")]
    [ApiController]
    [Authorize]
    public class SseController : ControllerBase
    {
        private readonly ISseService _sseService;

        public SseController(ISseService sseService)
        {
            _sseService = sseService;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

        // GET /api/v1/sse/stream — Server-Sent Events endpoint
        [HttpGet("stream")]
        public async Task Stream(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            await foreach (var sseEvent in _sseService.SubscribeToUser(userId, cancellationToken))
            {
                var data = JsonSerializer.Serialize(sseEvent.Data);
                await Response.WriteAsync($"event: {sseEvent.Type}\n", cancellationToken);
                await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}
