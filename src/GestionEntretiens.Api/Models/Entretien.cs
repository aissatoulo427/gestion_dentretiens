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

        /// <summary>
        /// Le tour que représente cet entretien (RH, Technique, Managerial).
        /// Le type qualifie le tour, pas la demande : une même demande enchaîne
        /// plusieurs tours de types différents.
        /// </summary>
        public TypeEntretien TypeEntretien { get; set; }

        // Provient d'1 demande (qui peut en produire plusieurs) et implique 1 candidat.
        // L'organisateur n'est pas dupliqué ici : c'est DemandeEntretien.RH.
        public int DemandeEntretienId { get; set; }
        public virtual DemandeEntretien DemandeEntretien { get; set; }

        public int CandidatId { get; set; }
        public virtual Candidat Candidat { get; set; }

        /// <summary>
        /// Le panel : les employés (RH et/ou managers) présents à cet entretien.
        /// Au moins un est exigé à la planification, et seuls eux peuvent en saisir
        /// le compte-rendu.
        /// </summary>
        public virtual ICollection<Employe> Evaluateurs { get; set; }

        // Se déroule sur le créneau réservé (renseigné à la planification).
        public int? CreneauId { get; set; }
        public virtual Creneau Creneau { get; set; }

        // Produit 0..n feedbacks. L'envoi des mails est délégué à IEmailService.
        public virtual ICollection<Feedback> Feedbacks { get; set; }

        public Entretien()
        {
            Evaluateurs = new List<Employe>();
            Feedbacks = new List<Feedback>();
            Statut = StatutEntretien.Planifie;
        }

        /// <summary>
        /// Associe le créneau réservé et fixe la modalité. La date n'est pas un paramètre :
        /// elle découle du créneau (<see cref="Creneau.DateDebut"/>), seule source de vérité
        /// de l'horaire. C'est ce qui rend impossible un entretien daté hors de son créneau.
        /// </summary>
        public bool Planifier(Creneau creneau, Modalite modalite, string lieuOuLien)
        {
            if (creneau == null || !creneau.Reserver())
            {
                return false;
            }
            Creneau = creneau;
            CreneauId = creneau.Id;
            DateHeure = creneau.DateDebut;
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

        /// <summary>Bascule sur un nouveau créneau (dont la date est reprise) et libère l'ancien.</summary>
        public bool Reprogrammer(Creneau nouveauCreneau)
        {
            if (nouveauCreneau == null || !nouveauCreneau.Reserver())
            {
                return false;
            }
            Creneau?.Liberer();
            Creneau = nouveauCreneau;
            CreneauId = nouveauCreneau.Id;
            DateHeure = nouveauCreneau.DateDebut;
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
