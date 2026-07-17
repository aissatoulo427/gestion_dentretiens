using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Api.Mapping;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

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

    /// <summary>Liste les feedbacks (compte-rendu) d'un entretien.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<FeedbackDto>> Get([FromQuery] int entretienId) =>
        Ok(_feedback.ConsulterCompteRendu(entretienId).Select(f => f.ToDto()));

    [HttpPost]
    public ActionResult<FeedbackDto> Saisir(SaisirFeedbackRequest req)
    {
        try
        {
            var f = _feedback.SaisirFeedback(req.EntretienId, req.AuteurId, req.Note, req.Commentaire, req.Decision);
            return Ok(f.ToDto());
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
        {
            return BadRequest(ex.Message);
        }
    }
}
