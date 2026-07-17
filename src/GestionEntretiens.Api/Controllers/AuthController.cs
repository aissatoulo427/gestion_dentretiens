using Gestion_dentretiens.Api.Dtos;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestion_dentretiens.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Authentifie un recruteur ou un manager (email + mot de passe) et renvoie un JWT.
    /// Endpoint public (pas besoin d'être déjà connecté).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest req)
    {
        var reponse = _auth.Login(req.Email, req.MotDePasse);
        return reponse is null ? Unauthorized("Email ou mot de passe invalide.") : Ok(reponse);
    }
}
