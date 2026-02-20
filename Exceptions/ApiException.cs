using FinanceSystem_Dotnet.Enums;

namespace FinanceSystem_Dotnet.Exceptions
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }
        public ErrorCode ErrorCode { get; }
        public Dictionary<string, object>? Args { get; }

        public ApiException(int statusCode, ErrorCode errorCode, Dictionary<string, object>? args = null)
            : base(errorCode.ToString())
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Args = args;
        }
    }
}
