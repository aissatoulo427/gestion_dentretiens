using System.Collections.Generic;

namespace Gestion_dentretiens.Models
{
    public class Candidat : Personne
    {
        public string Prenom { get; set; }
        public string Telephone { get; set; }

        // Un candidat est concerné par 0..n demandes et participe à 0..n entretiens.
        public virtual ICollection<DemandeEntretien> Demandes { get; set; }
        public virtual ICollection<Entretien> Entretiens { get; set; }

        public Candidat()
        {
            Demandes = new List<DemandeEntretien>();
            Entretiens = new List<Entretien>();
        }
    }
}
