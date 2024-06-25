using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace fpn_scale_api.Middleware
  {
  public class CustomExceptionHandlerMiddleware
    {
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

    public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
      {
      _next = next;
      _logger = logger;
      }

    public async Task InvokeAsync(HttpContext httpContext)
      {
      try
        {
        await _next(httpContext);
        }
      catch (Exception ex)
        {
        await HandleExceptionAsync(httpContext, ex, _logger);
        }
      }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
      {
      logger.LogError("[ERROR] An unhandled exception occurred: {message}", exception.Message);

      context.Response.ContentType = "application/json";

      if (exception is ArgumentException)
        {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        return context.Response.WriteAsync(new ErrorDetails
          {
          Success = false,
          Error = "Invalid scale id."
          }.ToString());
        }

      context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
      return context.Response.WriteAsync(new ErrorDetails
        {
        Success = false,
        Error = "Bad request."
        }.ToString());
      }
    }

  public class ErrorDetails
    {
    public bool Success { get; set; }
    public string Error { get; set; }

    public override string ToString()
      {
      return JsonSerializer.Serialize(this);
      }
    }
  }
