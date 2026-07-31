using Gestion_dentretiens.Api.Dtos;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Authentification : vérifie les identifiants (email + mot de passe), génère un JWT,
    /// et gère la réinitialisation du mot de passe par code OTP envoyé par e-mail.
    /// Seuls les employés (RH, évaluateurs techniques, managers) ont un compte.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Renvoie un JWT si les identifiants sont valides (et que la personne est un
        /// employé), sinon <c>null</c>.
        /// </summary>
        LoginResponse Login(string email, string motDePasse);

        /// <summary>
        /// Envoie le code permettant à un compte fraîchement créé de choisir son mot de
        /// passe. Appelé juste après la création d'un employé par l'administrateur.
        /// </summary>
        void DemanderActivation(string email);

        /// <summary>
        /// Vérifie le code d'activation et pose le mot de passe choisi par l'employé.
        /// Même vérification que <see cref="Reinitialiser"/> ; seul le message diffère.
        /// </summary>
        ApiMessage Activer(string email, string code, string motDePasse);

        /// <summary>
        /// Génère un code OTP, le stocke haché et l'envoie par e-mail.
        /// Ne renvoie rien et ne lève rien si l'email est inconnu : le contrôleur
        /// répond toujours 200 pour ne pas révéler quels comptes existent.
        /// </summary>
        void DemanderReinitialisation(string email);

        /// <summary>
        /// Vérifie le code OTP et remplace le mot de passe. Le code est à usage unique :
        /// il est effacé après un succès.
        /// </summary>
        ApiMessage Reinitialiser(string email, string code, string nouveauMotDePasse);
    }
}
