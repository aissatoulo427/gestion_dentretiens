using System;
using System.Collections.Generic;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Models
{
    public class Entretien
    {
        public int Id { get; set; }
        public DateTime DateHeure { get; set; }
        public string LieuOuLien { get; set; }
        public StatutEntretien Statut { get; set; }
        public Modalite Modalite { get; set; }

        // Provient d'1 demande (1-1 côté métier), implique 1 candidat, animé par 1 recruteur.
        public int DemandeEntretienId { get; set; }
        public virtual DemandeEntretien DemandeEntretien { get; set; }

        public int CandidatId { get; set; }
        public virtual Candidat Candidat { get; set; }

        public int RecruteurId { get; set; }
        public virtual Recruteur Recruteur { get; set; }

        // Se déroule sur le créneau réservé (renseigné à la planification).
        public int? CreneauId { get; set; }
        public virtual Creneau Creneau { get; set; }

        // Produit 0..n feedbacks. L'envoi des mails est délégué à IEmailService.
        public virtual ICollection<Feedback> Feedbacks { get; set; }

        public Entretien()
        {
            Feedbacks = new List<Feedback>();
            Statut = StatutEntretien.Planifie;
        }

        /// <summary>Associe le créneau réservé et fixe la date/modalité.</summary>
        public bool Planifier(Creneau creneau, DateTime dateHeure, Modalite modalite, string lieuOuLien)
        {
            if (creneau == null || !creneau.Reserver())
            {
                return false;
            }
            Creneau = creneau;
            CreneauId = creneau.Id;
            DateHeure = dateHeure;
            Modalite = modalite;
            LieuOuLien = lieuOuLien;
            Statut = StatutEntretien.Planifie;
            return true;
        }

        /// <summary>Confirme la participation (déclenché par le candidat).</summary>
        public void Confirmer()
        {
            Statut = StatutEntretien.Confirme;
        }

        /// <summary>Bascule sur un nouveau créneau et libère l'ancien.</summary>
        public bool Reprogrammer(Creneau nouveauCreneau, DateTime nouvelleDateHeure)
        {
            if (nouveauCreneau == null || !nouveauCreneau.Reserver())
            {
                return false;
            }
            Creneau?.Liberer();
            Creneau = nouveauCreneau;
            CreneauId = nouveauCreneau.Id;
            DateHeure = nouvelleDateHeure;
            Statut = StatutEntretien.Reprogramme;
            return true;
        }

        /// <summary>Annule l'entretien et libère le créneau associé.</summary>
        public void Annuler()
        {
            Creneau?.Liberer();
            Statut = StatutEntretien.Annule;
        }
    }
}
