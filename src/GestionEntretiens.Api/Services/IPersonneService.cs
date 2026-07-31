using System.Collections.Generic;
using Gestion_dentretiens.Models;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Gestion des personnes (candidats, admin, RH, évaluateurs techniques, managers).
    /// </summary>
    public interface IPersonneService
    {
        Candidat CreerCandidat(string nom, string prenom, string email, string telephone);

        /// <summary>Corrige la fiche d'un candidat (nom, prénom, e-mail, téléphone).</summary>
        Candidat ModifierCandidat(int id, string nom, string prenom, string email, string telephone);

        /// <summary>
        /// Supprime un candidat. Refuse s'il est lié à une demande ou à un entretien :
        /// c'est alors de l'historique, pas une fiche créée par erreur.
        /// </summary>
        void SupprimerCandidat(int id);

        // Les comptes employés se créent SANS mot de passe : leur titulaire le choisit
        // lui-même en activant son compte. Tant qu'il ne l'a pas fait, Login le refuse.
        RH CreerRH(string nom, string email);
        EvaluateurTechnique CreerEvaluateurTechnique(string nom, string email);
        Manager CreerManager(string nom, string email);

        /// <summary>
        /// Crée l'administrateur d'amorçage, seul compte à recevoir son mot de passe
        /// directement : il vient de la configuration, aucun e-mail n'entre en jeu. C'est
        /// ce qui garantit qu'une panne SMTP ne peut enfermer personne dehors.
        /// </summary>
        Admin CreerAdmin(string nom, string email, string motDePasse);

        /// <summary>Vrai s'il existe au moins un administrateur (test d'amorçage).</summary>
        bool ExisteUnAdmin();

        IEnumerable<Candidat> GetCandidats();
        IEnumerable<RH> GetRHs();
        IEnumerable<EvaluateurTechnique> GetEvaluateursTechniques();
        IEnumerable<Manager> GetManagers();
        Personne GetPersonne(int id);
    }
}
