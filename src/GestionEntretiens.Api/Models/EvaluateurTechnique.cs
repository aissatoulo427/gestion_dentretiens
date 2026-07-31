namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Celui qui fait passer le tour technique : développeur senior, architecte,
    /// référent data. Le nom décrit son rôle dans le recrutement et non son poste dans
    /// l'organigramme — il n'a aucune responsabilité hiérarchique, contrairement au
    /// <see cref="Manager"/>.
    ///
    /// Il pose ses créneaux et siège aux panels comme tout <see cref="Employe"/>.
    /// Ce qui le distingue tient dans une seule règle : un entretien de type Technique
    /// exige au moins un EvaluateurTechnique au panel.
    /// </summary>
    public class EvaluateurTechnique : Employe
    {
    }
}
