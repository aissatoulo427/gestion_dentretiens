using System;
using System.Collections.Generic;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Models
{
    public class DemandeEntretien
    {
        public int Id { get; set; }
        public string Poste { get; set; }
        public TypeEntretien TypeEntretien { get; set; }
        public DateTime DateCreation { get; set; }
        public StatutDemande Statut { get; set; }

        // Créée par 1 recruteur et concerne 1 candidat (clés étrangères explicites pour EF).
        public int RecruteurId { get; set; }
        public virtual Recruteur Recruteur { get; set; }

        public int CandidatId { get; set; }
        public virtual Candidat Candidat { get; set; }

        // Propose 0..n créneaux. La génération de l'entretien (1-1) est gérée par le service.
        public virtual ICollection<Creneau> Creneaux { get; set; }

        public DemandeEntretien()
        {
            Creneaux = new List<Creneau>();
            Statut = StatutDemande.Creee;
        }

        /// <summary>Marque la demande comme annulée (invariant d'état).</summary>
        public void Annuler()
        {
            Statut = StatutDemande.Annulee;
        }
    }
}
