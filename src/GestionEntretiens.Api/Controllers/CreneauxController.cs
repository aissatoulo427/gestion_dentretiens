using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

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
        return c is null ? NotFound() : Ok(c.ToDto());
    }

    /// <summary>Le recruteur définit une disponibilité (nouveau créneau).</summary>
    [HttpPost]
    public ActionResult<CreneauDto> DefinirDisponibilite(CreateCreneauRequest req)
    {
        try
        {
            var c = _planification.DefinirDisponibilite(req.RecruteurId, req.DateDebut, req.DateFin);
            return Ok(c.ToDto());
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Rattache un créneau à une demande (le propose au candidat).</summary>
    [HttpPost("{id:int}/proposer")]
    public IActionResult Proposer(int id, [FromQuery] int demandeId)
    {
        try
        {
            _planification.ProposerCreneau(demandeId, id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
