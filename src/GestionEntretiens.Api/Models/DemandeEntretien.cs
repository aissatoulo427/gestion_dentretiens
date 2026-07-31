using System;
using System.Collections.Generic;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Models
{
    public class DemandeEntretien
    {
        public int Id { get; set; }
        public string Poste { get; set; }
        public DateTime DateCreation { get; set; }
        public StatutDemande Statut { get; set; }

        // Ouverte par 1 RH et concerne 1 candidat (clés étrangères explicites pour EF).
        public int RhId { get; set; }
        public virtual RH RH { get; set; }

        public int CandidatId { get; set; }
        public virtual Candidat Candidat { get; set; }

        // Propose 0..n créneaux.
        public virtual ICollection<Creneau> Creneaux { get; set; }

        /// <summary>
        /// Les tours d'entretien issus de cette demande (RH → Technique → Managerial).
        /// Leur ordre découle de leurs DateHeure.
        /// </summary>
        public virtual ICollection<Entretien> Entretiens { get; set; }

        public DemandeEntretien()
        {
            Creneaux = new List<Creneau>();
            Entretiens = new List<Entretien>();
            Statut = StatutDemande.Creee;
        }

        /// <summary>Marque la demande comme annulée (invariant d'état).</summary>
        public void Annuler()
        {
            Statut = StatutDemande.Annulee;
        }
    }
}
