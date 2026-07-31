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
    /// Authentifie un recruteur ou un manager (email + mot de passe) et renvoie un JWT
    /// accompagné de l'identité de l'utilisateur (id, nom, email, rôle).
    /// Endpoint public (pas besoin d'être déjà connecté).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest req)
    {
        var reponse = _auth.Login(req.Email, req.MotDePasse);
        return reponse is null
            ? Unauthorized(ApiMessage.Erreur("Email ou mot de passe invalide."))
            : Ok(reponse);
    }

    /// <summary>
    /// Envoie par e-mail un code à 6 chiffres permettant de choisir un nouveau mot de passe.
    /// Répond toujours 200, même si l'e-mail est inconnu : sinon n'importe qui pourrait
    /// tester des adresses pour découvrir quels comptes existent.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("mot-de-passe-oublie")]
    public ActionResult<ApiMessage> MotDePasseOublie(MotDePasseOublieRequest req)
    {
        _auth.DemanderReinitialisation(req.Email);
        return Ok(ApiMessage.Ok("Si un compte existe pour cet e-mail, un code vient d'être envoyé."));
    }

    /// <summary>
    /// Vérifie le code reçu par e-mail et enregistre le nouveau mot de passe.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reinitialiser")]
    public ActionResult<ApiMessage> Reinitialiser(ReinitialiserRequest req)
    {
        var reponse = _auth.Reinitialiser(req.Email, req.Code, req.NouveauMotDePasse);
        return reponse.Succes ? Ok(reponse) : BadRequest(reponse);
    }

    /// <summary>
    /// Active un compte créé par l'administrateur : l'employé saisit le code reçu par
    /// e-mail et choisit son mot de passe. Techniquement identique à la réinitialisation
    /// — c'est le même code et la même preuve de possession de l'adresse — mais l'endroit
    /// séparé permet au front d'afficher un écran de bienvenue plutôt qu'un écran de
    /// réinitialisation, et rend l'activation visible dans Swagger.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("activer")]
    public ActionResult<ApiMessage> Activer(ActiverRequest req)
    {
        var reponse = _auth.Activer(req.Email, req.Code, req.NouveauMotDePasse);
        return reponse.Succes ? Ok(reponse) : BadRequest(reponse);
    }
}
