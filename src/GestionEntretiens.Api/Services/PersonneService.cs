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

        /// <summary>Crée un recruteur avec un mot de passe HACHÉ, et l'enregistre en base.</summary>
        public Recruteur CreerRecruteur(string nom, string email, string motDePasse)
        {
            var recruteur = new Recruteur { Nom = nom, Email = email };
            recruteur.MotDePasse = _hasher.HashPassword(recruteur, motDePasse);
            _db.Recruteurs.Add(recruteur);
            _db.SaveChanges();
            return recruteur;
        }

        /// <summary>Crée un manager avec un mot de passe HACHÉ, et l'enregistre en base.</summary>
        public Manager CreerManager(string nom, string email, string motDePasse)
        {
            var manager = new Manager { Nom = nom, Email = email };
            manager.MotDePasse = _hasher.HashPassword(manager, motDePasse);
            _db.Managers.Add(manager);
            _db.SaveChanges();
            return manager;
        }

        /// <summary>Renvoie tous les candidats (LINQ : ToList() exécute la requête SQL).</summary>
        public IEnumerable<Candidat> GetCandidats() => _db.Candidats.ToList();

        /// <summary>Renvoie tous les recruteurs.</summary>
        public IEnumerable<Recruteur> GetRecruteurs() => _db.Recruteurs.ToList();

        /// <summary>Renvoie tous les managers.</summary>
        public IEnumerable<Manager> GetManagers() => _db.Managers.ToList();

        /// <summary>Cherche une personne par son identifiant (Find = recherche par clé primaire).</summary>
        public Personne GetPersonne(int id) => _db.Personnes.Find(id);
    }
}
