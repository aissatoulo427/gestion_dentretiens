using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Envoi d'e-mails liés aux entretiens. C'est une action, sans entité persistée.
    /// </summary>
    public interface IEmailService
    {
        void Envoyer(string destinataire, string sujet, string corps);

        /// <summary>Envoie au candidat l'e-mail correspondant au type (invitation, rappel…).</summary>
        void NotifierEntretien(Entretien entretien, TypeNotification type);

        /// <summary>Envoie le code OTP de réinitialisation du mot de passe.</summary>
        void EnvoyerCodeReinitialisation(string destinataire, string code, int heuresValidite);

        /// <summary>Envoie le code permettant d'activer un compte nouvellement créé.</summary>
        void EnvoyerCodeActivation(string destinataire, string code, int joursValidite);
    }
}
