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

        // Porte sur 1 entretien, saisi par 1 employé qui était présent à cet entretien.
        public int EntretienId { get; set; }
        public virtual Entretien Entretien { get; set; }

        public int AuteurId { get; set; }
        public virtual Employe Auteur { get; set; }
    }
}
