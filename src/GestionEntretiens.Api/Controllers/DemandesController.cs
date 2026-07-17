using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

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
        return d is null ? NotFound() : Ok(d.ToDto());
    }

    [HttpPost]
    public ActionResult<DemandeDto> Creer(CreateDemandeRequest req)
    {
        try
        {
            var d = _planification.CreerDemande(req.RecruteurId, req.CandidatId, req.Poste, req.TypeEntretien);
            return CreatedAtAction(nameof(Get), new { id = d.Id }, d.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:int}/creneaux-disponibles")]
    public ActionResult<IEnumerable<CreneauDto>> CreneauxDisponibles(int id) =>
        Ok(_planification.ConsulterCreneauxDisponibles(id).Select(c => c.ToDto()));

    [HttpPost("{id:int}/annuler")]
    public IActionResult Annuler(int id)
    {
        try
        {
            _planification.AnnulerDemande(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
