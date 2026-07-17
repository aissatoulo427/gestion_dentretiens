namespace Gestion_dentretiens.Api.Dtos;

/// <summary>Identifiants envoyés au login.</summary>
public record LoginRequest(string Email, string MotDePasse);

/// <summary>Réponse du login : le token JWT à réutiliser dans l'en-tête Authorization.</summary>
public record LoginResponse(string Token, System.DateTime Expiration, string Role);
