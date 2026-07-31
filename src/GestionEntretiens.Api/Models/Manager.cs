namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// Le manager : le futur responsable hiérarchique du candidat. Il siège au tour
    /// managérial et y saisit son compte-rendu — un entretien de ce type exige sa
    /// présence au panel. Le tour technique revient désormais à
    /// <see cref="EvaluateurTechnique"/>.
    ///
    /// Il peut malgré tout siéger aux autres tours : la règle de composition impose une
    /// présence, elle n'exclut personne. Les créneaux et les entretiens sont hérités
    /// d'<see cref="Employe"/>.
    /// </summary>
    public class Manager : Employe
    {
    }
}
