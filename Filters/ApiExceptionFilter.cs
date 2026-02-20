using FinanceSystem_Dotnet.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceSystem_Dotnet.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is ApiException apiException)
            {
                var response = new
                {
                    statusCode = apiException.StatusCode,
                    errorCode = apiException.ErrorCode.ToString(),
                    message = apiException.ErrorCode.ToString(),
                    args = apiException.Args
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
