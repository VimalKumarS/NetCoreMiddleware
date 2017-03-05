using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;
using WebApplication_LearnMiddleware.cacheservice;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using WebApplication_LearnMiddleware.throttling;
using StackExchange.Redis;

namespace WebApplication_LearnMiddleware
{
    public class Startup 
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true);
            Configuration = builder.Build();
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddCors();
            services.AddRouting();
            var myoptions=Configuration.GetSection("MyMiddlewareOptionsSection");
            services.Configure<MyMiddlewareOptionsSection>(o => o.OptionOne = myoptions["OptionOne"]);

            services.AddDistributedRedisCache(options =>
            {
                options.InstanceName = "Sample";
                options.Configuration = "localhost";
            });
            services.AddMemoryCache();
            services.AddSession();
            services.AddSingleton<ICache,Cache>();


            //services.AddSingleton<IDistributedCache>(factory =>
            //{
            //    var cache = new RedisCache(new RedisCacheOptions
            //    {
            //        Configuration = redisConnection,
            //        InstanceName = redisInstance
            //    });

            //    return cache;
            //});

            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddTransient<CacheAttribute>();

            services.AddSingleton<IConnectionMultiplexer>(i => ConnectionMultiplexer.Connect("connection"));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors((builder) =>
            {
                builder.WithOrigins("http://localhost:5000");
            });

            app.UseSession();
            var routebuilder = new RouteBuilder(app);
            routebuilder.MapGet("greeting/{name}", appbiulder =>
            {
                appbiulder.Run(async context =>
                {
                    await context.Response.WriteAsync("greeting from my map branch");
                });
            });


            app.UseRouter(routebuilder.Build());
            //app.Use(async (AppContext, next) =>
            //{
            //    await AppContext.Response.WriteAsync("Hello from componenet 1");
            //    await next.Invoke();
            //    await AppContext.Response.WriteAsync("Hello from componenet 1");
            //});

            app.Map("/mymapbranch", (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await context.Response.WriteAsync("greeting from my map branch");
                });
            });

            app.UseMyMidlleware();

            app.MapWhen(context => context.Request.Query.ContainsKey("key"), appbuilder =>
             {
                 appbuilder.Run(async (context) =>
                 {
                     await context.Response.WriteAsync("map when called");
                 });
             });
            app.UseSimulatedLatency(
             min: TimeSpan.FromMilliseconds(100),
             max: TimeSpan.FromMilliseconds(300));

            app.UseMiddleware<CacheMiddleware>();

            app.UseRequestThrottling(requestsPerMinuteThreshold: 100);

            app.Run(async (context) =>
            {
               
                //var x= Cache. InvokeCached(() => this.sum(2, 2), new CachePolicy(new TimeSpan(0,0,60)));
                await context.Response.WriteAsync("Hello World!");
            });
        }

       
    }

    public class MyMiddlewareOptionsSection
    {
        public string OptionOne { get; set; }
    }
}
