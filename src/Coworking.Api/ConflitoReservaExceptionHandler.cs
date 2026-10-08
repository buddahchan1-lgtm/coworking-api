using Coworking.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Coworking.Api;

/// <summary>
/// Converte exceções de negócio em respostas ProblemDetails (RFC 7807):
/// ConflitoReservaException -> 409, KeyNotFoundException -> 404.
/// </summary>
public class ConflitoReservaExceptionHandler(IProblemDetailsService servicoProblemas)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto,
        Exception excecao, CancellationToken tokenCancelamento)
    {
        var (codigo, titulo) = excecao switch
        {
            ConflitoReservaException => (StatusCodes.Status409Conflict, "Conflito de horário"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            _ => (0, string.Empty)
        };

        if (codigo == 0)
            return false; // deixa o ProblemDetails padrão cuidar dos demais erros

        contexto.Response.StatusCode = codigo;

        return await servicoProblemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails
            {
                Status = codigo,
                Title = titulo,
                Detail = excecao.Message
            }
        });
    }
}
