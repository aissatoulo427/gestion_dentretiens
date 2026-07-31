namespace Gestion_dentretiens.Api.Dtos;

/// <summary>
/// Enveloppe unique de toutes les réponses qui ne renvoient pas de ressource :
/// erreurs métier et accusés de réception. Le front lit toujours <c>succes</c>
/// puis affiche <c>message</c>, quel que soit l'endpoint ou le code HTTP.
/// </summary>
public record ApiMessage(bool Succes, string Message)
{
    public static ApiMessage Erreur(string message) => new(false, message);
    public static ApiMessage Ok(string message) => new(true, message);
}
