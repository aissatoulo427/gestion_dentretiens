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
public class CreneauxController : ControllerBase
{
    private readonly IPlanificationService _planification;

    public CreneauxController(IPlanificationService planification)
    {
        _planification = planification;
    }

    /// <summary>Liste tous les créneaux.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<CreneauDto>> GetAll() =>
        Ok(_planification.GetCreneaux().Select(c => c.ToDto()));

    /// <summary>Lit un créneau par son identifiant.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<CreneauDto> Get(int id)
    {
        var c = _planification.GetCreneau(id);
        return c is null ? NotFound(ApiMessage.Erreur("Créneau introuvable.")) : Ok(c.ToDto());
    }

    /// <summary>
    /// L'employé connecté définit une disponibilité (nouveau créneau). Son identité est
    /// lue dans le JWT, pas dans le corps de la requête : le serveur n'a pas à croire
    /// l'appelant sur parole quand il déclare qui il est.
    /// Ouvert aux trois rôles qui font passer des entretiens : eux seuls ont des
    /// disponibilités à déclarer. L'administrateur en est écarté — il gère les comptes et
    /// ne participe à aucun recrutement.
    /// </summary>
    [Authorize(Roles = "RH,EvaluateurTechnique,Manager")]
    [HttpPost]
    public ActionResult<CreneauDto> DefinirDisponibilite(CreateCreneauRequest req)
    {
        if (!User.TryLireId(out var employeId))
            return Unauthorized(ApiMessage.Erreur("Token sans identifiant utilisateur."));

        var c = _planification.DefinirDisponibilite(employeId, req.DateDebut, req.DateFin);
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c.ToDto());
    }

    /// <summary>
    /// Supprime une de ses propres disponibilités — pour corriger une erreur de saisie ou
    /// signaler qu'on n'est plus libre.
    ///
    /// Réservé au propriétaire du créneau : c'est lui qui sait s'il est encore disponible,
    /// et personne d'autre n'a de raison d'effacer son agenda. D'où le 403 plutôt qu'une
    /// erreur métier — c'est une question de droit, pas de règle de gestion.
    ///
    /// Pour modifier l'horaire, supprimer puis recréer : un créneau n'a que des dates, il
    /// n'y a rien à conserver qui justifierait un PUT.
    /// </summary>
    [Authorize(Roles = "RH,EvaluateurTechnique,Manager")]
    [HttpDelete("{id:int}")]
    public ActionResult<ApiMessage> Supprimer(int id)
    {
        if (!User.TryLireId(out var employeId))
            return Unauthorized(ApiMessage.Erreur("Token sans identifiant utilisateur."));

        var creneau = _planification.GetCreneau(id);
        if (creneau is null) return NotFound(ApiMessage.Erreur("Créneau introuvable."));

        if (creneau.EmployeId != employeId)
            return StatusCode(403, ApiMessage.Erreur("Ce créneau ne vous appartient pas."));

        _planification.SupprimerCreneau(id);
        return Ok(ApiMessage.Ok("Créneau supprimé."));
    }

    /// <summary>
    /// Rattache un créneau à une demande (le propose au candidat). Réservé au RH, qui
    /// pilote la planification.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPost("{id:int}/proposer")]
    public ActionResult<ApiMessage> Proposer(int id, [FromQuery] int demandeId)
    {
        _planification.ProposerCreneau(demandeId, id);
        return Ok(ApiMessage.Ok("Créneau proposé à la demande."));
    }
}
