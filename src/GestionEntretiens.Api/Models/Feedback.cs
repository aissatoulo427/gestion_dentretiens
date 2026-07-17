using System;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public int Note { get; set; }
        public string Commentaire { get; set; }
        public Decision Decision { get; set; }
        public DateTime DateSaisie { get; set; }

        // Porte sur 1 entretien, saisi par 1 auteur (Recruteur ou Manager, donc Personne).
        public int EntretienId { get; set; }
        public virtual Entretien Entretien { get; set; }

        public int AuteurId { get; set; }
        public virtual Personne Auteur { get; set; }
    }
}
