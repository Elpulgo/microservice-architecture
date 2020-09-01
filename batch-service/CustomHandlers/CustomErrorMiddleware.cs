using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace batch_webservice
{
    public static class CustomErrorMiddleware
    {
        private const string ProblemMimeType = "application/problem+json";

        public static void UseCustomErrors(
            this IApplicationBuilder app,
            IHostEnvironment host)
        {
            if (host.IsDevelopment())
            {
                app.Use(WriteDevelopmentResponse);
                return;
            }

            // Use with details response here since it's just an example project.
            // WARNING! Should never include details (stacktrace) in prod though!
            app.Use(WriteDevelopmentResponse);
            // app.Use(WriteProductionResponse);
        }

        private static Task WriteDevelopmentResponse(HttpContext context, Func<Task> next)
            => WriteResponse(context, includeDetails: true, next);

        private static Task WriteProductionResponse(HttpContext context, Func<Task> next)
            => WriteResponse(context, includeDetails: false, next);

        private static async Task WriteResponse(HttpContext context, bool includeDetails, Func<Task> next)
        {
            var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionDetails?.Error;

            if (exception == null)
                return;

            context.Response.ContentType = ProblemMimeType;

            var title = includeDetails ? $"An error occured: {exception.Message}" : "An error occured";
            var details = includeDetails ? exception.ToString() : null;

            var problem = new ProblemDetails()
            {
                Title = title,
                Detail = details,
                Status = StatusCodes.Status500InternalServerError
            };

            var traceId = Activity.Current?.Id ?? context?.TraceIdentifier;
            if (traceId != null)
            {
                problem.Extensions["traceId"] = traceId;
            }

            var stream = context.Response.Body;
            await JsonSerializer.SerializeAsync(stream, problem);
        }
    }
}