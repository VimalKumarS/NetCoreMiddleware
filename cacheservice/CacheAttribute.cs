using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication_LearnMiddleware.cacheservice
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CacheAttribute : ResultFilterAttribute, IActionFilter
    {
        protected ICacheService CacheService { set; get; }

        public CacheAttribute()
        {

        }

        public int Duration { set; get; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            GetServices(context);
            var requestUrl = context.HttpContext.Request.GetEncodedUrl();
            //MD5 md5 = System.Security.Cryptography.MD5.Create();
            var cacheKey = CalculateMD5Hash(requestUrl);
            var cachedResult = CacheService.Get<string>(cacheKey);
            var contentType = CacheService.Get<string>(cacheKey + "_contentType");
            var statusCode = CacheService.Get<string>(cacheKey + "_statusCode");
            if (!string.IsNullOrEmpty(cachedResult) && !string.IsNullOrEmpty(contentType) &&
                !string.IsNullOrEmpty(statusCode))
            {
                //cache hit
                var httpResponse = context.HttpContext.Response;
                httpResponse.ContentType = contentType;
                httpResponse.StatusCode = Convert.ToInt32(statusCode);

                var responseStream = httpResponse.Body;
                responseStream.Seek(0, SeekOrigin.Begin);
                if (responseStream.Length <= cachedResult.Length)
                {
                    responseStream.SetLength((long)cachedResult.Length << 1);
                }
                using (var writer = new StreamWriter(responseStream, Encoding.UTF8, 4096, true))
                {
                    writer.Write(cachedResult);
                    writer.Flush();
                    responseStream.Flush();
                    context.Result = new ContentResult { Content = cachedResult };
                }
            }
            else
            {
                //cache miss
            }
        }

        public string CalculateMD5Hash(string input)

        {

            // step 1, calculate MD5 hash from input

            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)

            {

                sb.Append(hash[i].ToString("X2"));

            }

            return sb.ToString();

        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            //nothing for you there
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ContentResult)
            {
                context.Cancel = true;
            }
        }



        public override void OnResultExecuted(ResultExecutedContext context)
        {
            GetServices(context);
            var cacheKey = CalculateMD5Hash(context.HttpContext.Request.GetEncodedUrl());
            var httpResponse = context.HttpContext.Response;
            var responseStream = httpResponse.Body;
            responseStream.Seek(0, SeekOrigin.Begin);
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8, true, 512, true))
            {
                var toCache = streamReader.ReadToEnd();
                var contentType = httpResponse.ContentType;
                var statusCode = httpResponse.StatusCode.ToString();
                Task.Factory.StartNew(() =>
                {
                    CacheService.Store(cacheKey + "_contentType", contentType, Duration);
                    CacheService.Store(cacheKey + "_statusCode", statusCode, Duration);
                    CacheService.Store(cacheKey, toCache, Duration);
                });

            }
            base.OnResultExecuted(context);
        }
        protected void GetServices(FilterContext context)
        {
            CacheService = context.HttpContext.RequestServices.GetService(typeof(ICacheService)) as ICacheService;
        }
    }
}
