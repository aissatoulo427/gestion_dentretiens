using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

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
        return e is null ? NotFound() : Ok(e.ToDto());
    }

    /// <summary>Planifie l'entretien d'une demande sur un créneau (envoie l'invitation).</summary>
    [HttpPost]
    public ActionResult<EntretienDto> Planifier(PlanifierRequest req)
    {
        try
        {
            var e = _planification.PlanifierEntretien(req.DemandeId, req.CreneauId, req.DateHeure, req.Modalite, req.LieuOuLien);
            return CreatedAtAction(nameof(Get), new { id = e.Id }, e.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:int}/confirmer")]
    public IActionResult Confirmer(int id) => Execute(() => _planification.ConfirmerEntretien(id));

    [HttpPost("{id:int}/reprogrammer")]
    public IActionResult Reprogrammer(int id, ReprogrammerRequest req) =>
        Execute(() => _planification.Reprogrammer(id, req.NouveauCreneauId, req.NouvelleDateHeure));

    [HttpPost("{id:int}/rappel")]
    public IActionResult EnvoyerRappel(int id) => Execute(() => _planification.EnvoyerRappel(id));

    private IActionResult Execute(Action action)
    {
        try
        {
            action();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
