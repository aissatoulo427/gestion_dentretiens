using System.Collections.Generic;

namespace Gestion_dentretiens.Models
{
    public class Recruteur : Personne
    {
        // Un recruteur crée 0..n demandes, définit 0..n créneaux, anime 0..n entretiens.
        public virtual ICollection<DemandeEntretien> Demandes { get; set; }
        public virtual ICollection<Creneau> Creneaux { get; set; }
        public virtual ICollection<Entretien> Entretiens { get; set; }

        public Recruteur()
        {
            Demandes = new List<DemandeEntretien>();
            Creneaux = new List<Creneau>();
            Entretiens = new List<Entretien>();
        }
    }
}
