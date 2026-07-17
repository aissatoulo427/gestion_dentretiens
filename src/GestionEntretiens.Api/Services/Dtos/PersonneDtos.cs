namespace Gestion_dentretiens.Api.Dtos;

public record CandidatDto(int Id, string Nom, string Prenom, string Email, string Telephone);
public record CreateCandidatRequest(string Nom, string Prenom, string Email, string Telephone);

public record RecruteurDto(int Id, string Nom, string Email);
public record CreateRecruteurRequest(string Nom, string Email, string MotDePasse);

public record ManagerDto(int Id, string Nom, string Email);
public record CreateManagerRequest(string Nom, string Email, string MotDePasse);
