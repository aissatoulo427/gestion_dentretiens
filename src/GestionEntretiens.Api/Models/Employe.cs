using System;
using System.Collections.Generic;

namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Personne salariée de l'entreprise : RH, EvaluateurTechnique ou Manager.
    /// Ce sont les seules personnes qui possèdent un compte (donc un mot de passe)
    /// et les seules qui peuvent évaluer un entretien — un Candidat ne le peut pas,
    /// et c'est le typage qui l'interdit, pas une vérification à l'exécution.
    /// </summary>
    public abstract class Employe : Personne
    {
        /// <summary>Mot de passe HACHÉ (jamais en clair).</summary>
        public string MotDePasse { get; set; }

        // --- Réinitialisation du mot de passe par code OTP ---

        /// <summary>
        /// Code OTP HACHÉ (jamais en clair), valable pour une seule réinitialisation.
        /// null tant qu'aucune demande n'est en cours.
        /// </summary>
        public string CodeReinitialisation { get; set; }

        /// <summary>Date (UTC) au-delà de laquelle le code n'est plus accepté.</summary>
        public DateTime? ExpirationCode { get; set; }

        /// <summary>Nombre d'essais ratés sur le code en cours (limite le brute-force).</summary>
        public int TentativesCode { get; set; }

        /// <summary>Les entretiens où il siège comme évaluateur (N-N).</summary>
        public virtual ICollection<Entretien> Entretiens { get; set; }

        /// <summary>
        /// Ses disponibilités (0..n). Portées par Employe et non par un rôle précis :
        /// un entretien bloque le temps de celui qui le fait passer, quel qu'il soit.
        /// </summary>
        public virtual ICollection<Creneau> Creneaux { get; set; }

        protected Employe()
        {
            Entretiens = new List<Entretien>();
            Creneaux = new List<Creneau>();
        }
    }
}
