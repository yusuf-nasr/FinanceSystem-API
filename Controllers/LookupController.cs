using FinanceSystem_Dotnet.DTOs;
using FinanceSystem_Dotnet.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("api/v1/lookups")]
    [ApiController]
    public class LookupController : ControllerBase
    {
        [HttpGet]
        [Authorize]
        public ActionResult<LookupResponseDto> FindAll()
        {
            return Ok(new LookupResponseDto
            {
                UserRole = Enum.GetNames<Role>(),
                TransactionPriority = Enum.GetNames<TransactionPriority>(),
                TransactionForwardStatus = Enum.GetNames<TransactionForwardStatus>()
            });
        }
    }
}
