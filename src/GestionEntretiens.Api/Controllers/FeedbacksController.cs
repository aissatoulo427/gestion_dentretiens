using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Extensions;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

// Les erreurs métier remontent des services : ErreurMetierFilter les traduit en 400
// au format uniforme, d'où l'absence de try/catch ici.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedbacksController : ControllerBase
{
    private readonly IFeedbackService _feedback;

    public FeedbacksController(IFeedbackService feedback)
    {
        _feedback = feedback;
    }

    /// <summary>Liste les feedbacks (compte-rendu) d'un entretien : un par évaluateur.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<FeedbackDto>> Get([FromQuery] int entretienId) =>
        Ok(_feedback.ConsulterCompteRendu(entretienId).Select(f => f.ToDto()));

    /// <summary>
    /// Saisit un compte-rendu, signé par l'utilisateur connecté. L'auteur est lu dans le
    /// JWT et non dans le corps : sans ça, n'importe quel compte pouvait déposer un avis
    /// au nom d'un membre du panel. Le service vérifie ensuite que cet auteur siégeait
    /// bien à l'entretien.
    /// </summary>
    [HttpPost]
    public ActionResult<FeedbackDto> Saisir(SaisirFeedbackRequest req)
    {
        if (!User.TryLireId(out var auteurId))
            return Unauthorized(ApiMessage.Erreur("Token sans identifiant utilisateur."));

        var f = _feedback.SaisirFeedback(req.EntretienId, auteurId, req.Note, req.Commentaire, req.Decision);
        return Ok(f.ToDto());
    }
}
