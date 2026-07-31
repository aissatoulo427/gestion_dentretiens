namespace Gestion_dentretiens.Models
{
    /// <summary>
    /// L'administrateur : il crée et gère les comptes des employés, et rien d'autre.
    /// Il ne participe à aucun recrutement — ni demande, ni créneau, ni panel, ni
    /// compte-rendu. C'est ce périmètre, distinct des trois autres rôles, qui justifie
    /// une classe à part.
    ///
    /// Il existe pour l'amorçage : sans lui, aucun compte ne pourrait être créé sur une
    /// base vierge sans laisser un endpoint ouvert à tous. Il est créé au démarrage de
    /// l'application à partir de la configuration (voir Program.cs).
    ///
    /// Il hérite d'<see cref="Employe"/> pour le mot de passe et les champs OTP, et hérite
    /// donc aussi de Creneaux et Entretiens dont il ne se sert jamais. Impureté assumée :
    /// remonter ces champs sur Personne les donnerait au Candidat, qui n'a pas de compte.
    /// Les autorisations et la validation des panels l'écartent explicitement.
    /// </summary>
    public class Admin : Employe
    {
    }
}
