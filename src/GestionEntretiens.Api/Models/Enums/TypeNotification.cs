namespace Gestion_dentretiens.Models.Enums
{
    /// <summary>
    /// Sert uniquement à choisir le modèle d'e-mail à envoyer (pas d'entité persistée).
    /// </summary>
    public enum TypeNotification
    {
        Invitation,
        Rappel,
        Confirmation,
        Reprogrammation
    }
}
