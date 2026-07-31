namespace Gestion_dentretiens.Api.Dtos;

/// <summary>Identifiants envoyés au login.</summary>
public record LoginRequest(string Email, string MotDePasse);

/// <summary>
/// Réponse du login. <c>Token</c> est à renvoyer dans l'en-tête Authorization ;
/// <c>Id</c> est celui de l'utilisateur connecté, dont le front a besoin pour
/// alimenter recruteurId, auteurId, evaluateurIds… sans avoir à décoder le JWT.
/// </summary>
public record LoginResponse(string Token, System.DateTime Expiration,
    int Id, string Nom, string Email, string Role);

/// <summary>Demande d'envoi d'un code OTP pour réinitialiser le mot de passe.</summary>
public record MotDePasseOublieRequest(string Email);

/// <summary>Réinitialisation effective : le code reçu par e-mail + le nouveau mot de passe.</summary>
public record ReinitialiserRequest(string Email, string Code, string NouveauMotDePasse);

/// <summary>
/// Activation d'un compte : le code reçu par e-mail + le mot de passe choisi. Même forme
/// que <see cref="ReinitialiserRequest"/> — un type distinct pour que Swagger et le front
/// distinguent les deux intentions.
/// </summary>
public record ActiverRequest(string Email, string Code, string NouveauMotDePasse);
