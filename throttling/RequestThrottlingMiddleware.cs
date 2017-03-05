using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication_LearnMiddleware.throttling
{
    public class RequestThrottlingMiddleware
    {

        public IConnectionMultiplexer connection;

        private RequestDelegate next;

        private int requestsPerMinuteThreshold;

        public RequestThrottlingMiddleware(RequestDelegate next, IConnectionMultiplexer connection, int requestsPerMinuteThreshold)
        {
            this.next = next;
            this.connection = connection;
            this.requestsPerMinuteThreshold = requestsPerMinuteThreshold;
        }

        public async Task Invoke(HttpContext context)
        {
            var cache = connection.GetDatabase();

            // Get this from the context in whatever way the user supplies it
            var consumerKey = Guid.NewGuid().ToString();

            var consumerCacheKey = $"consumer.throttle#{consumerKey}";

            var cacheResult = cache.HashIncrement(consumerCacheKey, 1);

            if (cacheResult == 1)
            {
                cache.KeyExpire($"consumer.throttle#{consumerKey}", TimeSpan.FromSeconds(15), CommandFlags.FireAndForget);
            }
            else if (cacheResult > requestsPerMinuteThreshold)
            {
                context.Response.StatusCode = 429;

                using (var writer = new StreamWriter(context.Response.Body))
                {
                    await writer.WriteAsync("You are making too many requests.");
                }

                return;
            }

            await next(context);
        }
    }

    public static class RequestThrottlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestThrottling(
            this IApplicationBuilder app,
            int requestsPerMinuteThreshold = 100)
        {
            return app.UseMiddleware<RequestThrottlingMiddleware>(requestsPerMinuteThreshold);
        }
    }
}
