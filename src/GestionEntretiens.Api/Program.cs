using System.Text;
using System.Text.Json.Serialization;
using Gestion_dentretiens.Data;
using Gestion_dentretiens.Models;
using Gestion_dentretiens.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Base de données (EF Core + PostgreSQL) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDb")));

// --- Services métier ---
// Chaque service reçoit l'AppDbContext par injection de dépendances.
// (Pas de Repository/UnitOfWork : le DbContext EF Core l'est déjà.)
builder.Services.AddScoped<IPersonneService, PersonneService>();
builder.Services.AddScoped<IPlanificationService, PlanificationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

// --- Authentification : hachage des mots de passe + service de login (JWT) ---
builder.Services.AddScoped<IPasswordHasher<Personne>, PasswordHasher<Personne>>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
        };
    });

// --- Envoi d'e-mails (SMTP), configuré via appsettings ---
builder.Services.AddSingleton<IEmailService>(sp =>
{
    var smtp = builder.Configuration.GetSection("Smtp");
    return new SmtpEmailService(
        smtp["Host"] ?? "localhost",
        int.TryParse(smtp["Port"], out var port) ? port : 587,
        smtp["Expediteur"] ?? "",
        smtp["MotDePasse"] ?? "",
        bool.TryParse(smtp["Ssl"], out var ssl) ? ssl : true);
});

// --- API : controllers + enums en texte + dates au format uniforme ---
builder.Services.AddControllers(o =>
{
    // Traduit les exceptions métier des services en 400 { succes, message },
    // pour que toutes les erreurs de l'API aient la même forme.
    o.Filters.Add<Gestion_dentretiens.Api.Filters.ErreurMetierFilter>();
}).AddJsonOptions(o =>
{
    // Enums sérialisés en texte (ex. "Planifie" au lieu de 0).
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Dates au format uniforme "yyyy-MM-ddTHH:mm:ss" (même forme à la création et à la relecture).
    o.JsonSerializerOptions.Converters.Add(new Gestion_dentretiens.Api.Serialization.DateTimeJsonConverter());
});

// --- CORS : autorise un front (dev) à consommer l'API ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Bouton "Authorize" dans Swagger pour envoyer le token : en-tête "Authorization: Bearer <token>".
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Colle ton token JWT ici (sans le mot 'Bearer')."
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
    });
});

var app = builder.Build();

// --- Applique les migrations EF au démarrage (crée la base + les tables,
//     et applique les évolutions de schéma futures). ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // --- Amorçage de l'administrateur ---
    // Aucun endpoint ne crée de compte sans être authentifié : il faut donc que le premier
    // compte existe avant toute connexion. C'est le rôle de l'admin, seul compte à recevoir
    // son mot de passe directement (de la configuration), sans passer par un e-mail — ce qui
    // garantit qu'une panne SMTP ne peut enfermer personne dehors.
    var personnes = scope.ServiceProvider.GetRequiredService<IPersonneService>();
    if (!personnes.ExisteUnAdmin())
    {
        var email = builder.Configuration["Admin:Email"];
        var motDePasse = builder.Configuration["Admin:MotDePasse"];

        // Échec bruyant plutôt que silencieux : démarrer sans admin laisserait l'application
        // inutilisable — plus aucun compte ne pourrait être créé — sans rien qui l'explique.
        // La configuration n'est exigée QUE dans ce cas : une base déjà amorcée démarre sans.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse))
        {
            throw new InvalidOperationException(
                "Aucun administrateur en base et aucun compte d'amorçage configuré. " +
                "Renseignez Admin:Email et Admin:MotDePasse (secrets utilisateur en " +
                "développement, variables d'environnement en déploiement) puis relancez.");
        }

        personnes.CreerAdmin("Administrateur", email, motDePasse);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication(); // Lit et valide le JWT (doit être AVANT UseAuthorization).
app.UseAuthorization();
app.MapControllers();

app.Run();
