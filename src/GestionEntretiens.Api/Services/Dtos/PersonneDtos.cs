namespace Gestion_dentretiens.Api.Dtos;

public record CandidatDto(int Id, string Nom, string Prenom, string Email, string Telephone);
public record CreateCandidatRequest(string Nom, string Prenom, string Email, string Telephone);
public record UpdateCandidatRequest(string Nom, string Prenom, string Email, string Telephone);

// Aucune requête de création de compte ne porte de mot de passe : l'administrateur ne
// choisit pas le secret d'autrui. Le titulaire le pose lui-même via /auth/activer.
public record RHDto(int Id, string Nom, string Email);
public record CreateRHRequest(string Nom, string Email);

public record EvaluateurTechniqueDto(int Id, string Nom, string Email);
public record CreateEvaluateurTechniqueRequest(string Nom, string Email);

public record ManagerDto(int Id, string Nom, string Email);
public record CreateManagerRequest(string Nom, string Email);

public record AdminDto(int Id, string Nom, string Email);
