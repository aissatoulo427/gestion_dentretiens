using System;
using System.Collections.Generic;
using System.Linq;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    public class PlanificationService : IPlanificationService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        /// <summary>Reçoit le DbContext et le service d'e-mail par injection de dépendances.</summary>
        public PlanificationService(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        /// <summary>Cherche une demande par son identifiant.</summary>
        public DemandeEntretien GetDemande(int demandeId) => _db.Demandes.Find(demandeId);

        /// <summary>Renvoie toutes les demandes (LINQ : ToList() exécute la requête SQL).</summary>
        public IEnumerable<DemandeEntretien> GetDemandes() => _db.Demandes.ToList();

        /// <summary>Cherche un entretien par son identifiant.</summary>
        public Entretien GetEntretien(int entretienId) => _db.Entretiens.Find(entretienId);

        /// <summary>Renvoie tous les entretiens.</summary>
        public IEnumerable<Entretien> GetEntretiens() => _db.Entretiens.ToList();

        /// <summary>Crée une demande d'entretien après avoir vérifié que le recruteur et le candidat existent.</summary>
        public DemandeEntretien CreerDemande(int recruteurId, int candidatId, string poste, TypeEntretien type)
        {
            var recruteur = _db.Recruteurs.Find(recruteurId);
            if (recruteur == null) throw new InvalidOperationException("Recruteur introuvable.");
            var candidat = _db.Candidats.Find(candidatId);
            if (candidat == null) throw new InvalidOperationException("Candidat introuvable.");

            var demande = new DemandeEntretien
            {
                Poste = poste,
                TypeEntretien = type,
                DateCreation = DateTime.Now,
                Statut = StatutDemande.Creee,
                RecruteurId = recruteurId,
                CandidatId = candidatId
            };
            _db.Demandes.Add(demande);
            _db.SaveChanges();
            return demande;
        }

        /// <summary>Le recruteur définit un créneau de disponibilité (vérifie que fin &gt; début).</summary>
        public Creneau DefinirDisponibilite(int recruteurId, DateTime debut, DateTime fin)
        {
            if (fin <= debut) throw new ArgumentException("La fin doit être postérieure au début.");
            if (_db.Recruteurs.Find(recruteurId) == null) throw new InvalidOperationException("Recruteur introuvable.");

            var creneau = new Creneau
            {
                DateDebut = debut,
                DateFin = fin,
                Disponible = true,
                RecruteurId = recruteurId
            };
            _db.Creneaux.Add(creneau);
            _db.SaveChanges();
            return creneau;
        }

        /// <summary>Rattache un créneau existant à une demande (le recruteur le propose au candidat).</summary>
        public void ProposerCreneau(int demandeId, int creneauId)
        {
            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");
            var creneau = _db.Creneaux.Find(creneauId);
            if (creneau == null) throw new InvalidOperationException("Créneau introuvable.");

            creneau.DemandeEntretienId = demandeId;
            _db.SaveChanges();
        }

        /// <summary>Cherche un créneau par son identifiant.</summary>
        public Creneau GetCreneau(int creneauId) => _db.Creneaux.Find(creneauId);

        /// <summary>Renvoie tous les créneaux.</summary>
        public IEnumerable<Creneau> GetCreneaux() => _db.Creneaux.ToList();

        /// <summary>Renvoie les créneaux disponibles proposés pour une demande (LINQ : Where + ToList).</summary>
        public IEnumerable<Creneau> ConsulterCreneauxDisponibles(int demandeId)
        {
            return _db.Creneaux.Where(c => c.DemandeEntretienId == demandeId && c.Disponible).ToList();
        }

        /// <summary>
        /// Planifie l'entretien sur un créneau. Règle métier : un seul entretien par demande (1-1).
        /// Enregistre en base puis envoie l'e-mail d'invitation au candidat.
        /// </summary>
        public Entretien PlanifierEntretien(int demandeId, int creneauId, DateTime dateHeure, Modalite modalite, string lieuOuLien)
        {
            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");
            if (demande.Statut == StatutDemande.Annulee) throw new InvalidOperationException("Demande annulée.");

            // Règle métier : une seule planification par demande (cardinalité 1-1).
            if (_db.Entretiens.Any(e => e.DemandeEntretienId == demandeId))
                throw new InvalidOperationException("Un entretien existe déjà pour cette demande.");

            var creneau = _db.Creneaux.Find(creneauId);
            if (creneau == null) throw new InvalidOperationException("Créneau introuvable.");

            var entretien = new Entretien
            {
                DemandeEntretienId = demandeId,
                CandidatId = demande.CandidatId,
                RecruteurId = demande.RecruteurId
            };
            if (!entretien.Planifier(creneau, dateHeure, modalite, lieuOuLien))
                throw new InvalidOperationException("Le créneau n'est plus disponible.");

            _db.Entretiens.Add(entretien);
            demande.Statut = StatutDemande.Planifiee;
            _db.SaveChanges();

            entretien.Candidat = _db.Candidats.Find(demande.CandidatId);
            _email.NotifierEntretien(entretien, TypeNotification.Invitation);
            return entretien;
        }

        /// <summary>Confirme un entretien et envoie l'e-mail de confirmation au candidat.</summary>
        public void ConfirmerEntretien(int entretienId)
        {
            var entretien = _db.Entretiens.Find(entretienId);
            if (entretien == null) throw new InvalidOperationException("Entretien introuvable.");

            entretien.Confirmer();
            _db.SaveChanges();

            entretien.Candidat = _db.Candidats.Find(entretien.CandidatId);
            _email.NotifierEntretien(entretien, TypeNotification.Confirmation);
        }

        /// <summary>
        /// Reprogramme un entretien sur un nouveau créneau : libère l'ancien, réserve le nouveau,
        /// puis envoie l'e-mail de reprogrammation.
        /// </summary>
        public void Reprogrammer(int entretienId, int nouveauCreneauId, DateTime nouvelleDateHeure)
        {
            var entretien = _db.Entretiens.Find(entretienId);
            if (entretien == null) throw new InvalidOperationException("Entretien introuvable.");
            var nouveauCreneau = _db.Creneaux.Find(nouveauCreneauId);
            if (nouveauCreneau == null) throw new InvalidOperationException("Créneau introuvable.");

            // Libère l'ancien créneau explicitement (l'entité ne l'a pas forcément chargé).
            if (entretien.CreneauId.HasValue)
            {
                var ancien = _db.Creneaux.Find(entretien.CreneauId.Value);
                ancien?.Liberer();
            }

            if (!nouveauCreneau.Reserver())
                throw new InvalidOperationException("Le nouveau créneau n'est plus disponible.");

            entretien.Creneau = nouveauCreneau;
            entretien.CreneauId = nouveauCreneau.Id;
            entretien.DateHeure = nouvelleDateHeure;
            entretien.Statut = StatutEntretien.Reprogramme;
            _db.SaveChanges();

            entretien.Candidat = _db.Candidats.Find(entretien.CandidatId);
            _email.NotifierEntretien(entretien, TypeNotification.Reprogrammation);
        }

        /// <summary>Annule une demande et, s'il existe un entretien lié, libère son créneau et l'annule.</summary>
        public void AnnulerDemande(int demandeId)
        {
            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");

            demande.Annuler();
            var entretien = _db.Entretiens.Where(e => e.DemandeEntretienId == demandeId).FirstOrDefault();
            if (entretien != null && entretien.CreneauId.HasValue)
            {
                var creneau = _db.Creneaux.Find(entretien.CreneauId.Value);
                creneau?.Liberer();
                entretien.Statut = StatutEntretien.Annule;
            }
            _db.SaveChanges();
        }

        /// <summary>Envoie un e-mail de rappel au candidat pour un entretien à venir.</summary>
        public void EnvoyerRappel(int entretienId)
        {
            var entretien = _db.Entretiens.Find(entretienId);
            if (entretien == null) throw new InvalidOperationException("Entretien introuvable.");
            entretien.Candidat = _db.Candidats.Find(entretien.CandidatId);
            _email.NotifierEntretien(entretien, TypeNotification.Rappel);
        }
    }
}
