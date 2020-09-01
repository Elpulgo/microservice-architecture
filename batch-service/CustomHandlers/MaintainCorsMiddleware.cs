using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace batch_webservice
{
    public static class MaintainCorsMiddleware
    {
        private const string AccessControlHeaderPart = "access-control-";

        public static void MaintainCorsHeadersOnError(
            this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var corsHeaders = new HeaderDictionary();
                foreach (var pair in context.Response.Headers)
                {
                    if (!pair.Key.StartsWith(AccessControlHeaderPart, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    corsHeaders[pair.Key] = pair.Value;
                }

                context.Response.OnStarting(options =>
                {
                    var context = (HttpContext)options;
                    var headers = context.Response.Headers;

                    foreach (var pair in corsHeaders)
                    {
                        if (headers.ContainsKey(pair.Key))
                            continue;

                        headers.Add(pair.Key, pair.Value);
                    }

                    return Task.CompletedTask;
                }, context);

                await next();
            });
        }
    }
}