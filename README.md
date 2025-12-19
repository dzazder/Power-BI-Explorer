# Power BI Explorer

Aplikacja webowa w .NET do testowania i eksploracji Power BI API. Umożliwia interaktywne sprawdzanie wszystkich podstawowych metod API.

![Power BI Explorer](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![Power BI](https://img.shields.io/badge/Power%20BI-API-F2C811?style=flat-square&logo=powerbi)

## 🚀 Funkcjonalności

- **Token Management** - Generowanie i zarządzanie tokenami dostępu
- **Workspaces** - Przeglądanie wszystkich workspace'ów
- **Reports** - Lista raportów, szczegóły, embed configuration
- **Datasets** - Przeglądanie datasetów, historia odświeżeń, triggerowanie refresh
- **Dashboards** - Lista dashboardów i kafelków
- **Capacities** - Informacje o pojemnościach Premium
- **Gateways** - Lista bram danych
- **Dataflows** - Przeglądanie dataflows
- **API Tester** - Interaktywne testowanie dowolnych endpointów
- **Embed** - Generowanie embed tokenów dla raportów

## 📋 Wymagania

- .NET 10.0 SDK
- Konto Azure z zarejestrowaną aplikacją
- Licencja Power BI Pro lub Premium

## 🔧 Konfiguracja

### 1. Rejestracja aplikacji w Azure AD

1. Przejdź do [Azure Portal](https://portal.azure.com)
2. Otwórz **Azure Active Directory** → **App registrations** → **New registration**
3. Podaj nazwę aplikacji (np. "Power BI Explorer")
4. Wybierz "Accounts in this organizational directory only"
5. Kliknij **Register**

### 2. Konfiguracja uprawnień API

1. W zarejestrowanej aplikacji przejdź do **API permissions**
2. Kliknij **Add a permission** → **Power BI Service**
3. Wybierz **Application permissions** (dla Service Principal) lub **Delegated permissions**
4. Dodaj wymagane uprawnienia:
   - `Dataset.Read.All`
   - `Dataset.ReadWrite.All`
   - `Report.Read.All`
   - `Dashboard.Read.All`
   - `Workspace.Read.All`
   - `Capacity.Read.All`
   - `Gateway.Read.All`
   - `Dataflow.Read.All`
5. Kliknij **Grant admin consent**

### 3. Utworzenie Client Secret

1. Przejdź do **Certificates & secrets**
2. Kliknij **New client secret**
3. Podaj opis i wybierz okres ważności
4. **Skopiuj wartość secret** (będzie widoczna tylko raz!)

### 4. Konfiguracja aplikacji

Edytuj plik `appsettings.json`:

```json
{
  "PowerBI": {
    "ApplicationId": "YOUR-APPLICATION-ID",
    "ApplicationSecret": "YOUR-CLIENT-SECRET",
    "TenantId": "YOUR-TENANT-ID",
    "AuthorityUri": "https://login.microsoftonline.com/",
    "ResourceUrl": "https://analysis.windows.net/powerbi/api",
    "ApiUrl": "https://api.powerbi.com/",
    "Scope": "https://analysis.windows.net/powerbi/api/.default"
  }
}
```

### 5. Konfiguracja Service Principal w Power BI (opcjonalne)

Jeśli używasz uprawnień aplikacyjnych (Service Principal):

1. Zaloguj się do [Power BI Admin Portal](https://app.powerbi.com/admin-portal)
2. Przejdź do **Tenant settings**
3. Włącz **Allow service principals to use Power BI APIs**
4. Dodaj grupę bezpieczeństwa zawierającą Service Principal

## 🏃 Uruchomienie

```bash
# Przywróć pakiety
dotnet restore

# Uruchom aplikację
dotnet run

# Lub w trybie watch (hot reload)
dotnet watch run
```

Aplikacja będzie dostępna pod adresem: `https://localhost:5001` lub `http://localhost:5000`

## 📁 Struktura projektu

```
PowerBIExplorer/
├── Controllers/
│   └── PowerBIController.cs    # API endpoints
├── Models/
│   ├── ApiResponses.cs         # Modele odpowiedzi
│   ├── EmbedConfig.cs          # Konfiguracja embed
│   ├── PowerBIConfig.cs        # Konfiguracja połączenia
│   └── TokenResponse.cs        # Odpowiedź tokena
├── Services/
│   └── PowerBIService.cs       # Logika Power BI API
├── Pages/
│   ├── Index.cshtml            # Główna strona
│   └── Shared/
│       └── _Layout.cshtml      # Layout aplikacji
├── wwwroot/
│   ├── css/
│   │   └── site.css            # Style
│   └── js/
│       └── site.js             # JavaScript
├── Program.cs                   # Punkt wejścia
├── appsettings.json            # Konfiguracja
└── README.md
```

## 🔌 Dostępne endpointy API

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | `/api/powerbi/token` | Pobierz token dostępu |
| GET | `/api/powerbi/workspaces` | Lista workspace'ów |
| GET | `/api/powerbi/reports` | Raporty z My Workspace |
| GET | `/api/powerbi/workspaces/{id}/reports` | Raporty w workspace |
| GET | `/api/powerbi/workspaces/{id}/reports/{reportId}` | Szczegóły raportu |
| GET | `/api/powerbi/datasets` | Datasety z My Workspace |
| GET | `/api/powerbi/workspaces/{id}/datasets` | Datasety w workspace |
| GET | `/api/powerbi/dashboards` | Dashboardy z My Workspace |
| GET | `/api/powerbi/workspaces/{id}/dashboards` | Dashboardy w workspace |
| GET | `/api/powerbi/workspaces/{id}/dashboards/{id}/tiles` | Kafelki dashboardu |
| GET | `/api/powerbi/workspaces/{id}/datasets/{id}/refreshes` | Historia odświeżeń |
| POST | `/api/powerbi/workspaces/{id}/datasets/{id}/refresh` | Odśwież dataset |
| GET | `/api/powerbi/workspaces/{id}/reports/{id}/embed` | Embed config |
| GET | `/api/powerbi/capacities` | Lista capacities |
| GET | `/api/powerbi/gateways` | Lista gateways |
| GET | `/api/powerbi/workspaces/{id}/dataflows` | Dataflows w workspace |
| POST | `/api/powerbi/workspaces/{id}/reports/{id}/export` | Eksport raportu |

## 🎨 Interfejs użytkownika

Aplikacja posiada nowoczesny, ciemny interfejs z:
- Nawigacją boczną
- Interaktywnym testerem API
- Podświetlaniem składni JSON
- Powiadomieniami toast
- Statystykami na dashboardzie
- Responsywnym designem

## 🔐 Bezpieczeństwo

- Tokeny są cache'owane po stronie serwera
- Client Secret powinien być przechowywany bezpiecznie (np. Azure Key Vault)
- W produkcji użyj HTTPS
- Rozważ dodanie autentykacji użytkowników

## 📝 Licencja

MIT License

## 🤝 Wsparcie

W przypadku problemów:
1. Sprawdź konfigurację w Azure AD
2. Upewnij się, że uprawnienia API zostały zatwierdzone przez administratora
3. Sprawdź czy Service Principal ma dostęp do workspace'ów

