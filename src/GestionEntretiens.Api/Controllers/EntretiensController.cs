using Gestion_dentretiens.Api.Dtos;
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
public class EntretiensController : ControllerBase
{
    private readonly IPlanificationService _planification;

    public EntretiensController(IPlanificationService planification)
    {
        _planification = planification;
    }

    [HttpGet]
    public ActionResult<IEnumerable<EntretienDto>> GetAll() =>
        Ok(_planification.GetEntretiens().Select(e => e.ToDto()));

    [HttpGet("{id:int}")]
    public ActionResult<EntretienDto> Get(int id)
    {
        var e = _planification.GetEntretien(id);
        return e is null ? NotFound(ApiMessage.Erreur("Entretien introuvable.")) : Ok(e.ToDto());
    }

    /// <summary>
    /// Planifie un tour d'entretien sur un créneau, avec son panel d'évaluateurs
    /// (envoie l'invitation au candidat). Une demande peut en enchaîner plusieurs.
    /// Réservé au RH, qui pilote la planification. Le panel doit contenir au moins un
    /// évaluateur du rôle exigé par le type de tour, sinon 400.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPost]
    public ActionResult<EntretienDto> Planifier(PlanifierRequest req)
    {
        var e = _planification.PlanifierEntretien(req.DemandeId, req.CreneauId,
            req.Modalite, req.LieuOuLien, req.TypeEntretien, req.EvaluateurIds);
        return CreatedAtAction(nameof(Get), new { id = e.Id }, e.ToDto());
    }

    [HttpPost("{id:int}/confirmer")]
    public ActionResult<ApiMessage> Confirmer(int id)
    {
        _planification.ConfirmerEntretien(id);
        return Ok(ApiMessage.Ok("Entretien confirmé."));
    }

    [HttpPost("{id:int}/reprogrammer")]
    public ActionResult<ApiMessage> Reprogrammer(int id, ReprogrammerRequest req)
    {
        _planification.Reprogrammer(id, req.NouveauCreneauId);
        return Ok(ApiMessage.Ok("Entretien reprogrammé."));
    }

    [HttpPost("{id:int}/rappel")]
    public ActionResult<ApiMessage> EnvoyerRappel(int id)
    {
        _planification.EnvoyerRappel(id);
        return Ok(ApiMessage.Ok("Rappel envoyé au candidat."));
    }
}
