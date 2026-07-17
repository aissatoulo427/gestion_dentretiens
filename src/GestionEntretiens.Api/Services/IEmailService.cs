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
    }
}
