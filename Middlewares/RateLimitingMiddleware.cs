namespace FinanceSystem_Dotnet.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        private static int _count = 0;
        private static DateTime _lastReqTime = DateTime.Now;

        public RateLimitingMiddleware(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _count++;
            if(DateTime.Now.Subtract(_lastReqTime).Seconds > 10)
            {
                _count = 1;
                _lastReqTime = DateTime.Now;
                await _requestDelegate(context);
            }
            else
            {
                if(_count > 5)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                }
                else
                {
                    await _requestDelegate(context);
                }
            }
        }
    }
}
