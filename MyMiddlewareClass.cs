using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Options;

namespace WebApplication_LearnMiddleware
{
    public class MyMiddlewareClass
    {
        RequestDelegate _next;
        ILoggerFactory _loggerfactory;
        IOptions<MyMiddlewareOptionsSection> _options;
        private readonly ICache _cache;
        
        public MyMiddlewareClass(RequestDelegate next,ILoggerFactory loggerfactory,
            IOptions<MyMiddlewareOptionsSection> option, ICache cache)
        {
            _next = next;
            _options = option;
            _loggerfactory = loggerfactory;
            _cache = cache;
                //ctx.RequestServices.GetService<ICache>();
        }

        public async Task Invoke(HttpContext context)
        {
            var x = _cache.InvokeCached<int>(() => this.sum(2, 2), new CachePolicy(new TimeSpan(0, 0, 60)));
            _loggerfactory.AddConsole();
            var logger = _loggerfactory.CreateLogger("My own logger");
            logger.LogInformation("logger informaion ");
            await context.Response.WriteAsync(_options.Value.OptionOne);
            await _next.Invoke(context);
            
        }
        public int sum(int x, int y)
        {
            return x + y;
        }
    }

    public static class MyMiddlewareExtensions
    {
         public static IApplicationBuilder UseMyMidlleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MyMiddlewareClass>();
        }
    }
}
