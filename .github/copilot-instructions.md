# Project Guidelines

## Project Overview
Azure Function App HTTP Trigger pour tester la connectivité réseau avec logs Application Insights.

## Technology Stack
- **Runtime:** .NET 8.0 (Isolated Worker Model)
- **Framework:** Azure Functions v4
- **Cloud:** Microsoft Azure (Flexible Consumption Plan)
- **Logging:** Application Insights

## Key Features
- HTTP Trigger pour lancer les tests de connectivité
- Tests complets: DNS, TCP, SSL/TLS, Server Greeting
- Logs détaillés dans Application Insights
- Configuration via variables d'environnement ou requête JSON

## Development Guidelines
1. Utiliser `ILogger` pour tous les logs (automatiquement envoyés à Application Insights)
2. Retourner des réponses JSON structurées
3. Gérer les timeouts et exceptions gracieusement
4. Respecter les formats de log existants

## Testing
```bash
# Développement local
func start

# Tester localement
curl -X POST http://localhost:7071/api/ConnectivityTest \
  -H "Content-Type: application/json" \
  -d '{"server":"imap.gmail.com","port":"993"}'
```

## Deployment
```bash
# Compiler
dotnet publish -c Release

# Déployer
func azure functionapp publish <FunctionAppName>
```

## Logging & Monitoring
- Tous les logs vont automatiquement à Application Insights
- Accès via Azure Portal → Application Insights → Logs/Search
- Utiliser `ILogger` pour tous les messages
