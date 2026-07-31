using System.Collections.Generic;

namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Le RH : il enregistre les candidats, ouvre les demandes d'entretien et pilote
    /// la planification. Il évalue aussi le tour RH — un entretien de ce type exige
    /// sa présence au panel.
    /// Les créneaux qu'il pose et les entretiens où il siège sont hérités
    /// d'<see cref="Employe"/> : ce ne sont pas des privilèges de RH.
    /// </summary>
    public class RH : Employe
    {
        // Seul le RH ouvre des demandes (0..n).
        public virtual ICollection<DemandeEntretien> Demandes { get; set; }

        public RH()
        {
            Demandes = new List<DemandeEntretien>();
        }
    }
}
