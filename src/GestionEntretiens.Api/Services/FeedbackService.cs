using System;
using System.Collections.Generic;
using System.Linq;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Models.Enums;
using Microsoft.EntityFrameworkCore;

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
        /// Vérifie que la note est valide (0..5) et que l'auteur faisait bien partie du
        /// panel de cet entretien : on ne commente que ce à quoi on a assisté.
        /// </summary>
        public Feedback SaisirFeedback(int entretienId, int auteurId, int note, string commentaire, Decision decision)
        {
            // InvalidOperationException et non ArgumentOutOfRangeException : cette dernière
            // suffixe le message avec "(Parameter 'note')", qui finirait affiché à l'utilisateur.
            if (note < 0 || note > 5)
                throw new InvalidOperationException("La note doit être comprise entre 0 et 5.");

            // Include : il faut charger le panel pour pouvoir le tester.
            var entretien = _db.Entretiens
                .Include(e => e.Evaluateurs)
                .FirstOrDefault(e => e.Id == entretienId);
            if (entretien == null) throw new InvalidOperationException("Entretien introuvable.");

            if (!entretien.Evaluateurs.Any(e => e.Id == auteurId))
                throw new InvalidOperationException("Seul un évaluateur présent à l'entretien peut saisir un compte-rendu.");

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
