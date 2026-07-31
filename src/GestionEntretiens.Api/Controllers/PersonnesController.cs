using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Il faut être connecté (staff), sauf pour l'inscription d'un recruteur/manager.
public class PersonnesController : ControllerBase
{
    private readonly IPersonneService _service;
    private readonly IAuthService _auth;

    public PersonnesController(IPersonneService service, IAuthService auth)
    {
        _service = service;
        _auth = auth;
    }

    // Les trois créations de compte suivent le même schéma : créer sans mot de passe, puis
    // envoyer le code d'activation. Les deux opérations sont distinctes — si l'envoi échoue,
    // le compte existe sans que son titulaire ait reçu de code. Ce n'est pas bloquant : il en
    // obtient un neuf via /auth/mot-de-passe-oublie. Les rendre transactionnelles n'y
    // changerait rien, un envoi d'e-mail ne se rejoue pas dans une transaction.

    // --- Candidats ---
    [HttpGet("candidats")]
    public ActionResult<IEnumerable<CandidatDto>> GetCandidats() =>
        Ok(_service.GetCandidats().Select(c => c.ToDto()));

    /// <summary>
    /// Enregistre un candidat. Réservé au RH : le sourcing fait partie de l'administratif
    /// du recrutement, que le RH pilote de bout en bout.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPost("candidats")]
    public ActionResult<CandidatDto> CreerCandidat(CreateCandidatRequest req)
    {
        var c = _service.CreerCandidat(req.Nom, req.Prenom, req.Email, req.Telephone);
        return CreatedAtAction(nameof(GetPersonne), new { id = c.Id }, c.ToDto());
    }

    /// <summary>
    /// Corrige la fiche d'un candidat. Réservé au RH, comme sa création.
    /// Utile surtout pour une adresse e-mail mal saisie : sans ça, le candidat ne reçoit
    /// jamais ses invitations et rien ne permet de le rectifier.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpPut("candidats/{id:int}")]
    public ActionResult<CandidatDto> ModifierCandidat(int id, UpdateCandidatRequest req)
    {
        var c = _service.ModifierCandidat(id, req.Nom, req.Prenom, req.Email, req.Telephone);
        return Ok(c.ToDto());
    }

    /// <summary>
    /// Supprime un candidat créé par erreur. Refuse s'il est déjà engagé dans un
    /// recrutement — c'est alors de l'historique, pas une erreur de saisie.
    /// </summary>
    [Authorize(Roles = "RH")]
    [HttpDelete("candidats/{id:int}")]
    public ActionResult<ApiMessage> SupprimerCandidat(int id)
    {
        _service.SupprimerCandidat(id);
        return Ok(ApiMessage.Ok("Candidat supprimé."));
    }

    // --- RH ---
    [HttpGet("rh")]
    public ActionResult<IEnumerable<RHDto>> GetRHs() =>
        Ok(_service.GetRHs().Select(r => r.ToDto()));

    [Authorize(Roles = "Admin")]
    [HttpPost("rh")]
    public ActionResult<RHDto> CreerRH(CreateRHRequest req)
    {
        var r = _service.CreerRH(req.Nom, req.Email);
        _auth.DemanderActivation(r.Email);
        return CreatedAtAction(nameof(GetPersonne), new { id = r.Id }, r.ToDto());
    }

    // --- Évaluateurs techniques ---
    [HttpGet("evaluateurs-techniques")]
    public ActionResult<IEnumerable<EvaluateurTechniqueDto>> GetEvaluateursTechniques() =>
        Ok(_service.GetEvaluateursTechniques().Select(e => e.ToDto()));

    [Authorize(Roles = "Admin")]
    [HttpPost("evaluateurs-techniques")]
    public ActionResult<EvaluateurTechniqueDto> CreerEvaluateurTechnique(CreateEvaluateurTechniqueRequest req)
    {
        var e = _service.CreerEvaluateurTechnique(req.Nom, req.Email);
        _auth.DemanderActivation(e.Email);
        return CreatedAtAction(nameof(GetPersonne), new { id = e.Id }, e.ToDto());
    }

    // --- Managers ---
    [HttpGet("managers")]
    public ActionResult<IEnumerable<ManagerDto>> GetManagers() =>
        Ok(_service.GetManagers().Select(m => m.ToDto()));

    [Authorize(Roles = "Admin")]
    [HttpPost("managers")]
    public ActionResult<ManagerDto> CreerManager(CreateManagerRequest req)
    {
        var m = _service.CreerManager(req.Nom, req.Email);
        _auth.DemanderActivation(m.Email);
        return CreatedAtAction(nameof(GetPersonne), new { id = m.Id }, m.ToDto());
    }

    // --- Lecture par id ---
    [HttpGet("{id:int}")]
    public IActionResult GetPersonne(int id)
    {
        var p = _service.GetPersonne(id);
        // Type vaut "RH", "EvaluateurTechnique", "Manager" ou "Candidat" : le nom réel
        // de la sous-classe, fiable tant que les proxies EF ne sont pas activés.
        return p is null
            ? NotFound(ApiMessage.Erreur("Personne introuvable."))
            : Ok(new { p.Id, p.Nom, p.Email, Type = p.GetType().Name });
    }
}
