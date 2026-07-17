using System.Collections.Generic;
using Gestion_dentretiens.Models;

namespace Gestion_dentretiens.Services
{
    /// <summary>
    /// Gestion des personnes (candidats, recruteurs, managers).
    /// </summary>
    public interface IPersonneService
    {
        Candidat CreerCandidat(string nom, string prenom, string email, string telephone);
        Recruteur CreerRecruteur(string nom, string email, string motDePasse);
        Manager CreerManager(string nom, string email, string motDePasse);

        IEnumerable<Candidat> GetCandidats();
        IEnumerable<Recruteur> GetRecruteurs();
        IEnumerable<Manager> GetManagers();
        Personne GetPersonne(int id);
    }
}
