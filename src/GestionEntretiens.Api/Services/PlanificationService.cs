using System;
using System.Collections.Generic;
using System.Linq;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// Corrige l'intitulé du poste d'une demande. Seul champ modifiable : le recruteur,
        /// le candidat et la date de création identifient la demande, les changer en ferait
        /// une autre. Pour tout le reste, on annule et on recrée.
        /// </summary>
        public DemandeEntretien ModifierPoste(int demandeId, string poste)
        {
            if (string.IsNullOrWhiteSpace(poste))
                throw new InvalidOperationException("Le poste est obligatoire.");

            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");

            demande.Poste = poste;
            _db.SaveChanges();
            return demande;
        }

        /// <summary>Renvoie toutes les demandes (LINQ : ToList() exécute la requête SQL).</summary>
        public IEnumerable<DemandeEntretien> GetDemandes() => _db.Demandes.ToList();

        /// <summary>Cherche un entretien par son identifiant, panel compris.</summary>
        public Entretien GetEntretien(int entretienId) =>
            _db.Entretiens.Include(e => e.Evaluateurs).FirstOrDefault(e => e.Id == entretienId);

        /// <summary>Renvoie tous les entretiens, panel compris.</summary>
        public IEnumerable<Entretien> GetEntretiens() =>
            _db.Entretiens.Include(e => e.Evaluateurs).ToList();

        /// <summary>
        /// Crée une demande d'entretien après avoir vérifié que le recruteur et le candidat existent.
        /// Le type de tour n'est pas fixé ici : il est choisi à chaque planification.
        /// </summary>
        public DemandeEntretien CreerDemande(int rhId, int candidatId, string poste)
        {
            var rh = _db.RHs.Find(rhId);
            if (rh == null) throw new InvalidOperationException("RH introuvable.");
            var candidat = _db.Candidats.Find(candidatId);
            if (candidat == null) throw new InvalidOperationException("Candidat introuvable.");

            var demande = new DemandeEntretien
            {
                Poste = poste,
                DateCreation = DateTime.Now,
                Statut = StatutDemande.Creee,
                RhId = rhId,
                CandidatId = candidatId
            };
            _db.Demandes.Add(demande);
            _db.SaveChanges();
            return demande;
        }

        /// <summary>L'employé définit un créneau de disponibilité (vérifie que fin &gt; début).</summary>
        public Creneau DefinirDisponibilite(int employeId, DateTime debut, DateTime fin)
        {
            if (fin <= debut) throw new ArgumentException("La fin doit être postérieure au début.");
            if (_db.Employes.Find(employeId) == null) throw new InvalidOperationException("Employé introuvable.");

            var creneau = new Creneau
            {
                DateDebut = debut,
                DateFin = fin,
                Disponible = true,
                EmployeId = employeId
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

        /// <summary>
        /// Supprime un créneau. Refuse dans deux cas, qui sont distincts :
        ///
        /// 1. le créneau est réservé (<c>Disponible == false</c>) : un entretien s'y tient ;
        /// 2. un entretien le référence encore, même s'il a été libéré. C'est le cas après
        ///    l'annulation d'une demande : <c>AnnulerDemande</c> appelle <c>Liberer()</c>,
        ///    donc Disponible repasse à true, mais <c>Entretien.CreneauId</c> continue de
        ///    pointer dessus. Tester Disponible seul laisserait passer la suppression, qui
        ///    échouerait alors sur la clé étrangère (OnDelete.Restrict) en 500.
        ///
        /// L'appelant a déjà vérifié que le créneau lui appartient.
        /// </summary>
        public void SupprimerCreneau(int creneauId)
        {
            var creneau = _db.Creneaux.Find(creneauId);
            if (creneau == null) throw new InvalidOperationException("Créneau introuvable.");

            if (!creneau.Disponible)
                throw new InvalidOperationException(
                    "Ce créneau est réservé par un entretien. Utilisez la reprogrammation pour le libérer.");

            if (_db.Entretiens.Any(e => e.CreneauId == creneauId))
                throw new InvalidOperationException(
                    "Ce créneau reste rattaché à un entretien passé ou annulé, il ne peut pas être supprimé.");

            _db.Creneaux.Remove(creneau);
            _db.SaveChanges();
        }

        /// <summary>Renvoie les créneaux disponibles proposés pour une demande (LINQ : Where + ToList).</summary>
        public IEnumerable<Creneau> ConsulterCreneauxDisponibles(int demandeId)
        {
            return _db.Creneaux.Where(c => c.DemandeEntretienId == demandeId && c.Disponible).ToList();
        }

        /// <summary>
        /// Planifie un tour d'entretien sur un créneau. Une demande peut en enchaîner
        /// plusieurs (RH → Technique → Managerial), chacun avec son propre panel.
        /// Enregistre en base puis envoie l'e-mail d'invitation au candidat.
        /// </summary>
        public Entretien PlanifierEntretien(int demandeId, int creneauId,
            Modalite modalite, string lieuOuLien, TypeEntretien type, IEnumerable<int> evaluateurIds)
        {
            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");
            if (demande.Statut == StatutDemande.Annulee) throw new InvalidOperationException("Demande annulée.");

            // R1 : un entretien se tient devant au moins un évaluateur.
            var ids = (evaluateurIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (ids.Count == 0)
                throw new InvalidOperationException("Un entretien doit compter au moins un évaluateur.");

            // Le DbSet Employes écarte d'office les candidats : si le compte manque à
            // l'appel, c'est qu'un des identifiants n'est pas un recruteur ou un manager.
            var evaluateurs = _db.Employes.Where(e => ids.Contains(e.Id)).ToList();
            if (evaluateurs.Count != ids.Count)
                throw new InvalidOperationException("Un évaluateur est introuvable ou n'est pas un employé.");

            // L'administrateur est un Employe (il lui faut un compte), donc il passe le test
            // ci-dessus. Mais il ne participe à aucun recrutement : sans ce refus, la règle
            // de composition ci-dessous le laisserait entrer en second évaluateur, puisqu'elle
            // exige une présence sans exclure personne — et il pourrait ensuite saisir un
            // compte-rendu, seuls les membres du panel y étant autorisés.
            if (evaluateurs.Any(e => e is Admin))
                throw new InvalidOperationException("Un administrateur ne peut pas évaluer un entretien.");

            // R2 : chaque type de tour exige au moins un évaluateur du rôle correspondant.
            // « Au moins un », pas « seulement » : un tour technique peut aussi réunir le
            // manager et le RH. La règle impose une présence, elle n'exclut personne.
            // `is` plutôt que GetType() : si les proxies EF étaient activés un jour,
            // GetType() renverrait EvaluateurTechniqueProxy et la règle rejetterait tout.
            bool panelValide = type switch
            {
                TypeEntretien.RH => evaluateurs.Any(e => e is RH),
                TypeEntretien.Technique => evaluateurs.Any(e => e is EvaluateurTechnique),
                TypeEntretien.Managerial => evaluateurs.Any(e => e is Manager),
                _ => false
            };
            if (!panelValide)
                throw new InvalidOperationException(
                    $"Un entretien {type} exige au moins un évaluateur du rôle correspondant dans le panel.");

            var creneau = _db.Creneaux.Find(creneauId);
            if (creneau == null) throw new InvalidOperationException("Créneau introuvable.");

            var entretien = new Entretien
            {
                DemandeEntretienId = demandeId,
                CandidatId = demande.CandidatId,
                TypeEntretien = type
            };
            foreach (var evaluateur in evaluateurs)
            {
                entretien.Evaluateurs.Add(evaluateur);
            }

            if (!entretien.Planifier(creneau, modalite, lieuOuLien))
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
        public void Reprogrammer(int entretienId, int nouveauCreneauId)
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
            entretien.DateHeure = nouveauCreneau.DateDebut;
            entretien.Statut = StatutEntretien.Reprogramme;
            _db.SaveChanges();

            entretien.Candidat = _db.Candidats.Find(entretien.CandidatId);
            _email.NotifierEntretien(entretien, TypeNotification.Reprogrammation);
        }

        /// <summary>
        /// Annule une demande, ainsi que TOUS les tours d'entretien qui en découlent, en
        /// libérant leurs créneaux.
        ///
        /// Le pluriel est essentiel : depuis le passage aux tours multiples, une demande
        /// enchaîne un entretien RH, un technique, un managérial. La version précédente
        /// n'en traitait qu'un seul (FirstOrDefault) — les autres restaient « Planifié » et
        /// leurs créneaux réservés définitivement, plus rien ne venant les libérer.
        /// </summary>
        public void AnnulerDemande(int demandeId)
        {
            var demande = _db.Demandes.Find(demandeId);
            if (demande == null) throw new InvalidOperationException("Demande introuvable.");

            demande.Annuler();

            var entretiens = _db.Entretiens.Where(e => e.DemandeEntretienId == demandeId).ToList();
            foreach (var entretien in entretiens)
            {
                if (entretien.Statut == StatutEntretien.Annule) continue;

                if (entretien.CreneauId.HasValue)
                {
                    var creneau = _db.Creneaux.Find(entretien.CreneauId.Value);
                    creneau?.Liberer();
                }

                // Hors du test sur le créneau, contrairement à la version précédente : un
                // entretien sans créneau devait lui aussi être annulé, il ne l'était pas.
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
