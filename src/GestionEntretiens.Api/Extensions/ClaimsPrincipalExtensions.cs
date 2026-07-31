using System.Security.Claims;

namespace Gestion_dentretiens.Api.Extensions;

/// <summary>
/// Lecture de l'identité portée par le JWT.
///
/// Principe : quand une action doit être attribuée à l'utilisateur connecté (le
/// propriétaire d'un créneau, l'auteur d'un compte-rendu), l'identifiant se lit ici et
/// jamais dans le corps de la requête. Un client peut écrire ce qu'il veut dans un JSON ;
/// il ne peut pas fabriquer un token signé. C'est donc le serveur qui décide qui agit.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Identifiant de l'utilisateur connecté, tiré du claim NameIdentifier posé au login
    /// par <c>AuthService.GenererToken</c>. Renvoie false si le claim est absent ou
    /// illisible : ce n'est pas une erreur métier mais un défaut d'authentification,
    /// que l'appelant traduit en 401.
    /// </summary>
    public static bool TryLireId(this ClaimsPrincipal user, out int id) =>
        int.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
