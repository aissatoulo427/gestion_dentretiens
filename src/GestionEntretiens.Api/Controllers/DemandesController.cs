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
public class DemandesController : ControllerBase
{
    private readonly IPlanificationService _planification;

    public DemandesController(IPlanificationService planification)
    {
        _planification = planification;
    }

    [HttpGet]
    public ActionResult<IEnumerable<DemandeDto>> GetAll() =>
        Ok(_planification.GetDemandes().Select(d => d.ToDto()));

    [HttpGet("{id:int}")]
    public ActionResult<DemandeDto> Get(int id)
    {
        var d = _planification.GetDemande(id);
        return d is null ? NotFound(ApiMessage.Erreur("Demande introuvable.")) : Ok(d.ToDto());
    }

    /// <summary>
    /// Le RH connecté ouvre une demande pour un candidat. L'organisateur est lu dans le
    /// JWT et non dans le corps : on n'ouvre pas un recrutement au nom d'un collègue.
    /// Réservé au rôle RH, car une demande appartient forcément à un RH (clé étrangère
    /// DemandeEntretien.RhId).
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPost]
    public ActionResult<DemandeDto> Creer(CreateDemandeRequest req)
    {
        if (!User.TryLireId(out var rhId))
            return Unauthorized(ApiMessage.Erreur("Token sans identifiant utilisateur."));

        var d = _planification.CreerDemande(rhId, req.CandidatId, req.Poste);
        return CreatedAtAction(nameof(Get), new { id = d.Id }, d.ToDto());
    }

    /// <summary>
    /// Corrige l'intitulé du poste. Les autres champs ne sont pas modifiables : le RH, le
    /// candidat et la date de création identifient la demande — les changer en ferait une
    /// autre, autant l'annuler et en ouvrir une nouvelle.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPut("{id:int}")]
    public ActionResult<DemandeDto> Modifier(int id, UpdateDemandeRequest req)
    {
        var d = _planification.ModifierPoste(id, req.Poste);
        return Ok(d.ToDto());
    }

    [HttpGet("{id:int}/creneaux-disponibles")]
    public ActionResult<IEnumerable<CreneauDto>> CreneauxDisponibles(int id) =>
        Ok(_planification.ConsulterCreneauxDisponibles(id).Select(c => c.ToDto()));

    [HttpPost("{id:int}/annuler")]
    public ActionResult<ApiMessage> Annuler(int id)
    {
        _planification.AnnulerDemande(id);
        return Ok(ApiMessage.Ok("Demande annulée."));
    }
}
