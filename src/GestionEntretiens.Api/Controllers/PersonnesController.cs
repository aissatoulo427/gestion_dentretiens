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

    public PersonnesController(IPersonneService service)
    {
        _service = service;
    }

    // --- Candidats ---
    [HttpGet("candidats")]
    public ActionResult<IEnumerable<CandidatDto>> GetCandidats() =>
        Ok(_service.GetCandidats().Select(c => c.ToDto()));

    [HttpPost("candidats")]
    public ActionResult<CandidatDto> CreerCandidat(CreateCandidatRequest req)
    {
        var c = _service.CreerCandidat(req.Nom, req.Prenom, req.Email, req.Telephone);
        return CreatedAtAction(nameof(GetPersonne), new { id = c.Id }, c.ToDto());
    }

    // --- Recruteurs ---
    [HttpGet("recruteurs")]
    public ActionResult<IEnumerable<RecruteurDto>> GetRecruteurs() =>
        Ok(_service.GetRecruteurs().Select(r => r.ToDto()));

    [AllowAnonymous] // Inscription : permet de créer le premier compte pour ensuite se connecter.
    [HttpPost("recruteurs")]
    public ActionResult<RecruteurDto> CreerRecruteur(CreateRecruteurRequest req)
    {
        var r = _service.CreerRecruteur(req.Nom, req.Email, req.MotDePasse);
        return CreatedAtAction(nameof(GetPersonne), new { id = r.Id }, r.ToDto());
    }

    // --- Managers ---
    [HttpGet("managers")]
    public ActionResult<IEnumerable<ManagerDto>> GetManagers() =>
        Ok(_service.GetManagers().Select(m => m.ToDto()));

    [AllowAnonymous] // Inscription : permet de créer le premier compte pour ensuite se connecter.
    [HttpPost("managers")]
    public ActionResult<ManagerDto> CreerManager(CreateManagerRequest req)
    {
        var m = _service.CreerManager(req.Nom, req.Email, req.MotDePasse);
        return CreatedAtAction(nameof(GetPersonne), new { id = m.Id }, m.ToDto());
    }

    // --- Lecture par id ---
    [HttpGet("{id:int}")]
    public IActionResult GetPersonne(int id)
    {
        var p = _service.GetPersonne(id);
        return p is null ? NotFound() : Ok(new { p.Id, p.Nom, p.Email, Type = p.GetType().Name });
    }
}
