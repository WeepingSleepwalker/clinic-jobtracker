using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

public static class ProblemDetailsExtensions
{
    public static IResult ToProblem(this (int status, string title, string? detail, string? instance) p)
    {
        var (status, title, detail, instance) = p;
        return Results.Problem(type: "about:blank", title: title, detail: detail,
                               statusCode: status, instance: instance);
    }
}
