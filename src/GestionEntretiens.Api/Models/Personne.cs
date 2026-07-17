namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Classe mère abstraite pour tous les acteurs humains (Candidat, Recruteur, Manager).
    /// </summary>
    public abstract class Personne
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Email { get; set; }

        /// <summary>
        /// Mot de passe HACHÉ (jamais en clair). Rempli uniquement pour les comptes
        /// qui se connectent (Recruteur, Manager) ; null pour les candidats.
        /// </summary>
        public string MotDePasse { get; set; }
    }
}
