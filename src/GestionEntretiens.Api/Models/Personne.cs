namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Classe mère abstraite pour tous les acteurs humains.
    /// Deux branches : le Candidat (pas de compte) et l'Employe (RH, EvaluateurTechnique,
    /// Manager).
    /// </summary>
    public abstract class Personne
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string Email { get; set; }
    }
}
