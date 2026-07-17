using System;
using System.Collections.Generic;
using System.Linq;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;

namespace Gestion_dentretiens.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly AppDbContext _db;

        /// <summary>Reçoit le DbContext par injection de dépendances.</summary>
        public FeedbackService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Enregistre un feedback pour un entretien.
        /// Vérifie que la note est valide (0..5) et que l'auteur est un recruteur ou un manager.
        /// </summary>
        public Feedback SaisirFeedback(int entretienId, int auteurId, int note, string commentaire, Decision decision)
        {
            if (note < 0 || note > 5) throw new ArgumentOutOfRangeException(nameof(note), "La note doit être comprise entre 0 et 5.");

            var entretien = _db.Entretiens.Find(entretienId);
            if (entretien == null) throw new InvalidOperationException("Entretien introuvable.");

            var auteur = _db.Personnes.Find(auteurId);
            if (!(auteur is Recruteur || auteur is Manager))
                throw new InvalidOperationException("Seul un recruteur ou un manager peut saisir un feedback.");

            var feedback = new Feedback
            {
                EntretienId = entretienId,
                AuteurId = auteurId,
                Note = note,
                Commentaire = commentaire,
                Decision = decision,
                DateSaisie = DateTime.Now
            };
            _db.Feedbacks.Add(feedback);
            _db.SaveChanges();
            return feedback;
        }

        /// <summary>Renvoie tous les feedbacks d'un entretien (LINQ : Where filtre, ToList exécute).</summary>
        public IEnumerable<Feedback> ConsulterCompteRendu(int entretienId)
        {
            return _db.Feedbacks.Where(f => f.EntretienId == entretienId).ToList();
        }
    }
}
