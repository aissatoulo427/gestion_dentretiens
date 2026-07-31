# Configuration — démarrer le projet sur une nouvelle machine

Ce fichier est **versionné** : il liste les réglages nécessaires. Les **valeurs**, elles, ne
sont jamais dans le dépôt — chacun les pose sur sa machine.

## Où vivent les secrets

Dans les **secrets utilisateur .NET**, un fichier stocké hors du dépôt :

```
%APPDATA%\Microsoft\UserSecrets\1657b270-92ac-4e2a-88f1-10fd9c48e454\secrets.json
```

L'identifiant vient de `<UserSecretsId>` dans `GestionEntretiens.Api.csproj`, il est donc le
même pour tout le monde. Le dossier, lui, est propre à chaque machine.

Pourquoi pas un `.env` à la racine ? Parce qu'un fichier posé dans le dépôt finit tôt ou tard
committé — un `git add -f`, un outil mal réglé, un `.gitignore` écrasé. Hors du dépôt, c'est
impossible par construction. Et ASP.NET Core lit les secrets utilisateur nativement, sans
qu'on ait une ligne de code à écrire.

## Voir ce qui est déjà configuré

```
dotnet user-secrets list --project src/GestionEntretiens.Api
```

## Les clés à renseigner

| Clé | À quoi ça sert | Sans elle |
|---|---|---|
| `ConnectionStrings:AppDb` | connexion PostgreSQL | l'application ne démarre pas |
| `Jwt:Key` | signature des tokens (32 caractères minimum) | aucune connexion possible |
| `Admin:Email` | compte administrateur créé au démarrage | démarrage refusé tant qu'aucun admin n'existe |
| `Admin:MotDePasse` | mot de passe de ce compte | idem |
| `Smtp:Expediteur` | adresse d'envoi des e-mails | e-mails écrits dans la console au lieu d'être envoyés |
| `Smtp:MotDePasse` | mot de passe SMTP | idem |

Les clés `Smtp` sont **facultatives**. Sans elles, `SmtpEmailService` écrit le message dans la
console préfixé par `[MAIL non envoyé]` au lieu de planter. En développement c'est même
pratique : les codes d'activation et de réinitialisation restent lisibles sans boîte mail.

Les clés `Admin`, elles, sont obligatoires **au premier démarrage seulement**. Aucun endpoint
public ne crée de compte : sans cet administrateur d'amorçage, personne ne pourrait jamais se
connecter. Une fois le compte créé en base, ces clés ne servent plus et l'application démarre
sans elles.

## Les poser

```
dotnet user-secrets set "ConnectionStrings:AppDb" "Host=...;Database=...;Username=...;Password=..." --project src/GestionEntretiens.Api
dotnet user-secrets set "Jwt:Key" "une-chaine-d-au-moins-32-caracteres" --project src/GestionEntretiens.Api
dotnet user-secrets set "Admin:Email" "admin@exemple.com" --project src/GestionEntretiens.Api
dotnet user-secrets set "Admin:MotDePasse" "MotDePasseSolide1!" --project src/GestionEntretiens.Api

# facultatif
dotnet user-secrets set "Smtp:Expediteur" "envoi@exemple.com" --project src/GestionEntretiens.Api
dotnet user-secrets set "Smtp:MotDePasse" "..." --project src/GestionEntretiens.Api
```

Les valeurs de `ConnectionStrings:AppDb` et des clés `Smtp` sont à demander à l'autre membre
de l'équipe — par un canal privé, jamais par le dépôt. `Jwt:Key` et les clés `Admin` peuvent
être différentes sur chaque machine, elles n'ont pas besoin d'être partagées.

## Premier démarrage

```
dotnet run --project src/GestionEntretiens.Api
```

Les migrations EF s'appliquent automatiquement, puis l'administrateur est créé s'il n'existe
pas encore. Si une clé `Admin` manque, le démarrage échoue avec un message qui la nomme —
c'est volontaire : démarrer sans administrateur laisserait l'application inutilisable sans
rien qui l'explique.

Connecte-toi ensuite en admin via `POST /api/auth/login`, crée les comptes RH et évaluateurs,
et chacun activera le sien avec le code reçu par e-mail (`POST /api/auth/activer`).
