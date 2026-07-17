using System.Collections.Generic;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Saisie et consultation des feedbacks (comptes-rendus) d'entretien.
    /// </summary>
    public interface IFeedbackService
    {
        Feedback SaisirFeedback(int entretienId, int auteurId, int note, string commentaire, Decision decision);
        IEnumerable<Feedback> ConsulterCompteRendu(int entretienId);
    }
}
