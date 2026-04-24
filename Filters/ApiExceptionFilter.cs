using FinanceSystem_Dotnet.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace FinanceSystem_Dotnet.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is ApiException apiException)
            {
                // Match Node's error response shape:
                // { statusCode, message: { key: "ERROR_CODE", ...args }, error: "Http Status Text" }
                var messageObj = new Dictionary<string, object>
                {
                    { "key", apiException.ErrorCode.ToString() }
                };

                if (apiException.Args != null)
                {
                    foreach (var kv in apiException.Args)
                        messageObj[kv.Key] = kv.Value;
                }

                var httpStatus = (HttpStatusCode)apiException.StatusCode;
                var errorText = httpStatus.ToString(); // e.g. "Unauthorized", "Forbidden", "NotFound"

                var response = new
                {
                    statusCode = apiException.StatusCode,
                    message = messageObj,
                    error = errorText
                };

                context.Result = new ObjectResult(response)
                {
                    StatusCode = apiException.StatusCode
                };
                context.ExceptionHandled = true;
            }
        }
    }
}
