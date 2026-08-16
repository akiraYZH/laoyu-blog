
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace laoyu_blog_backend.Exceptions;

public sealed class SlugConflictExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!IsSlugConflict(exception))
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Slug already exists.",
            Detail = "A blog post with this slug already exists.",
            Status = StatusCodes.Status409Conflict
        };

        httpContext.Response.StatusCode =
            StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static bool IsSlugConflict(
        Exception exception)
    {
        return exception is DbUpdateException dbUpdateException
            && dbUpdateException.InnerException
                is PostgresException postgresException
            && postgresException.SqlState
                == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName
                == "IX_BlogPosts_Slug";
    }
}