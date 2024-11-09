using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Core.Middleware.ExceptionHandling;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (BaseErrorCodeException e)
        {
            await HandleException(httpContext, e);
        }
    }
    
    private async Task HandleException(HttpContext context, Exception exception)
    {
        var responseObject = JsonSerializer.Serialize(new {exception.Message, Code = 400});

        var exceptionData  = Encoding.UTF8.GetBytes(responseObject );
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.Body.WriteAsync(exceptionData, 0, exceptionData.Length, CancellationToken.None);
    }
}