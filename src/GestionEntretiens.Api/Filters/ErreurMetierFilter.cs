using System;
using Gestion_dentretiens.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gestion_dentretiens.Api.Filters;

/// <summary>
/// Traduit les exceptions métier des services en réponse 400 uniforme.
/// Les contrôleurs n'ont donc plus de try/catch : ils laissent remonter, et
/// toutes les erreurs de l'API sortent avec la même forme JSON.
/// Les exceptions non métier ne sont pas interceptées : elles doivent rester
/// des 500, sinon on masquerait de vrais bugs derrière un 400.
/// </summary>
public class ErreurMetierFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        // ArgumentOutOfRangeException dérive d'ArgumentException : elle est couverte.
        if (context.Exception is InvalidOperationException or ArgumentException)
        {
            context.Result = new BadRequestObjectResult(ApiMessage.Erreur(context.Exception.Message));
            context.ExceptionHandled = true;
        }
    }
}
