using Gestion_dentretiens.Api.Dtos;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Authentification : vérifie les identifiants (email + mot de passe) et génère un JWT.
    /// Seuls les Recruteurs et Managers ont un compte.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Renvoie un JWT si les identifiants sont valides (et que la personne est
        /// Recruteur ou Manager), sinon <c>null</c>.
        /// </summary>
        LoginResponse Login(string email, string motDePasse);
    }
}
