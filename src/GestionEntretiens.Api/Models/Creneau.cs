using System;

namespace Gestion_dentretiens.Models
{
    public class Creneau
    {
        public int Id { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public bool Disponible { get; set; }

        // Défini par 1 employé, quel que soit son rôle : RH, évaluateur technique ou
        // manager posent tous leurs disponibilités.
        public int EmployeId { get; set; }
        public virtual Employe Employe { get; set; }

        // Optionnellement proposé pour une demande (0..n créneaux par demande).
        public int? DemandeEntretienId { get; set; }
        public virtual DemandeEntretien DemandeEntretien { get; set; }

        public Creneau()
        {
            Disponible = true;
        }

        /// <summary>Réserve le créneau. Retourne false s'il était déjà pris.</summary>
        public bool Reserver()
        {
            if (!Disponible)
            {
                return false;
            }
            Disponible = false;
            return true;
        }

        /// <summary>Libère le créneau.</summary>
        public void Liberer()
        {
            Disponible = true;
        }
    }
}
