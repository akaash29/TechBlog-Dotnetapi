using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace UserRegistration.Middleware;

/// <summary>
/// Replaces ASP.NET Core's default authorization result handling so that authentication (401)
/// and authorization (403) failures are reported in the same ProblemDetails JSON shape as
/// <see cref="GlobalExceptionHandler"/> uses for thrown exceptions, instead of the framework's
/// default empty response bodies. Registered in place of <see cref="IAuthorizationMiddlewareResultHandler"/>,
/// this runs as part of the `UseAuthorization()` middleware — a distinct stage from JWT
/// authentication (`UseAuthentication()`), which only establishes who the caller is.
/// </summary>
public sealed class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            return WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.");
        }

        if (authorizeResult.Forbidden)
        {
            return WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource.");
        }

        return next(context);
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}
