using System.Collections.Generic;
using System.Linq;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Microsoft.AspNetCore.Identity;

namespace Gestion_dentretiens.Services
{
    public class PersonneService : IPersonneService
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Personne> _hasher;

        /// <summary>
        /// Reçoit le DbContext et le hacheur de mot de passe par injection de dépendances.
        /// </summary>
        public PersonneService(AppDbContext db, IPasswordHasher<Personne> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        /// <summary>Crée un candidat et l'enregistre en base (Add puis SaveChanges).</summary>
        public Candidat CreerCandidat(string nom, string prenom, string email, string telephone)
        {
            var candidat = new Candidat { Nom = nom, Prenom = prenom, Email = email, Telephone = telephone };
            _db.Candidats.Add(candidat);
            _db.SaveChanges();
            return candidat;
        }

        /// <summary>
        /// Crée un RH sans mot de passe : il le choisira en activant son compte.
        /// MotDePasse reste null, ce qui suffit à rendre le compte inutilisable —
        /// <c>AuthService.Login</c> refuse tout compte sans mot de passe.
        /// </summary>
        public RH CreerRH(string nom, string email)
        {
            var rh = new RH { Nom = nom, Email = email };
            _db.RHs.Add(rh);
            _db.SaveChanges();
            return rh;
        }

        /// <summary>Crée un évaluateur technique sans mot de passe (activation par e-mail).</summary>
        public EvaluateurTechnique CreerEvaluateurTechnique(string nom, string email)
        {
            var evaluateur = new EvaluateurTechnique { Nom = nom, Email = email };
            _db.EvaluateursTechniques.Add(evaluateur);
            _db.SaveChanges();
            return evaluateur;
        }

        /// <summary>Crée un manager sans mot de passe (activation par e-mail).</summary>
        public Manager CreerManager(string nom, string email)
        {
            var manager = new Manager { Nom = nom, Email = email };
            _db.Managers.Add(manager);
            _db.SaveChanges();
            return manager;
        }

        /// <summary>
        /// Crée l'administrateur d'amorçage, avec son mot de passe HACHÉ. Seul compte à en
        /// recevoir un directement : il vient de la configuration, sans passer par un e-mail.
        /// </summary>
        public Admin CreerAdmin(string nom, string email, string motDePasse)
        {
            var admin = new Admin { Nom = nom, Email = email };
            admin.MotDePasse = _hasher.HashPassword(admin, motDePasse);
            _db.Admins.Add(admin);
            _db.SaveChanges();
            return admin;
        }

        /// <summary>Vrai s'il existe au moins un administrateur (test d'amorçage).</summary>
        public bool ExisteUnAdmin() => _db.Admins.Any();

        /// <summary>
        /// Corrige la fiche d'un candidat. Cas courant et jusqu'ici sans solution :
        /// une adresse e-mail mal saisie, qui empêche le candidat de recevoir ses
        /// invitations sans qu'aucun moyen existe de la rectifier.
        /// </summary>
        public Candidat ModifierCandidat(int id, string nom, string prenom, string email, string telephone)
        {
            var candidat = _db.Candidats.Find(id);
            if (candidat == null) throw new InvalidOperationException("Candidat introuvable.");

            candidat.Nom = nom;
            candidat.Prenom = prenom;
            candidat.Email = email;
            candidat.Telephone = telephone;
            _db.SaveChanges();
            return candidat;
        }

        /// <summary>
        /// Supprime un candidat, à condition qu'il ne soit engagé dans aucun recrutement.
        /// Une fiche liée à une demande ou à un entretien fait partie de l'historique : la
        /// supprimer effacerait le contexte de comptes-rendus déjà écrits — et échouerait
        /// de toute façon sur les clés étrangères (OnDelete.Restrict), en 500 au lieu d'un
        /// message clair.
        /// </summary>
        public void SupprimerCandidat(int id)
        {
            var candidat = _db.Candidats.Find(id);
            if (candidat == null) throw new InvalidOperationException("Candidat introuvable.");

            if (_db.Demandes.Any(d => d.CandidatId == id))
                throw new InvalidOperationException(
                    "Ce candidat a des demandes d'entretien, il ne peut pas être supprimé.");

            if (_db.Entretiens.Any(e => e.CandidatId == id))
                throw new InvalidOperationException(
                    "Ce candidat a des entretiens, il ne peut pas être supprimé.");

            _db.Candidats.Remove(candidat);
            _db.SaveChanges();
        }

        /// <summary>Renvoie tous les candidats (LINQ : ToList() exécute la requête SQL).</summary>
        public IEnumerable<Candidat> GetCandidats() => _db.Candidats.ToList();

        /// <summary>Renvoie tous les RH.</summary>
        public IEnumerable<RH> GetRHs() => _db.RHs.ToList();

        /// <summary>Renvoie tous les évaluateurs techniques.</summary>
        public IEnumerable<EvaluateurTechnique> GetEvaluateursTechniques() =>
            _db.EvaluateursTechniques.ToList();

        /// <summary>Renvoie tous les managers.</summary>
        public IEnumerable<Manager> GetManagers() => _db.Managers.ToList();

        /// <summary>Cherche une personne par son identifiant (Find = recherche par clé primaire).</summary>
        public Personne GetPersonne(int id) => _db.Personnes.Find(id);
    }
}
