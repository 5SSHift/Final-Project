# ShopManager — Final Project

Aplicație desktop dezvoltată în cadrul proiectului final, implementând un sistem complet de gestiune pentru un magazin online. Proiectul acoperă autentificare securizată cu amprentă de dispozitiv, gestionarea produselor, comenzilor și utilizatorilor, cu suport pentru două limbi (română / engleză).

---

## Cuprins

- [Funcționalități](#-funcționalități)
- [Arhitectură](#-arhitectură)
- [Tehnologii](#-tehnologii)
- [Cerințe sistem](#-cerințe-sistem)
- [Instalare și configurare](#-instalare-și-configurare)
- [Structura proiectului](#-structura-proiectului)
- [Roluri utilizatori](#-roluri-utilizatori)
- [Securitate](#-securitate)

---

## Funcționalități

### Autentificare & Securitate
- Autentificare cu username și parolă (hash PBKDF2-HMAC-SHA512, 310.000 iterații)
- Token de sesiune semnat RSA-2048 / SHA-256, valabil 8 ore
- **Device Fingerprinting** — amprentă unică per stație de lucru (SHA-256)
- **Auto-login** la repornire sau **autentificare manuală** (setabil per utilizator)
- Deconectare cu invalidarea înregistrării dispozitivului

### Magazin (Client)
- Catalog produse cu căutare în timp real
- Filtrare după categorie și oferte speciale
- Coș de cumpărături cu calcul automat reduceri
- Plasare comenzi cu adresă de livrare și metodă de plată

### Gestiune Produse (Employee / Admin)
- CRUD complet produse (nume, categorie, preț, stoc, reducere, imagine)
- Upload imagine produs (stocat ca `VARBINARY(MAX)` în SQL Server)
- Reîncărcare listă în timp real

### Gestiune Utilizatori (Admin)
- Creare utilizatori cu parolă setată de administrator
- Filtrare multi-selecție după rol (Administrator / Angajat / Client)
- Editare și dezactivare conturi

### Dashboard
- Statistici în timp real (produse, comenzi, utilizatori, valoare stoc)
- Informații token RSA activ (utilizator, rol, expirare)
- Status conexiune SQL Server

### Setări
- **Schimbare limbă** în timp real  Română  / Engleză 
- Configurare preferință autentificare la pornire
- Test conexiune bază de date
- Informații sistem (versiune, framework)

---

## Arhitectură

Proiectul urmează pattern-ul **MVVM** (Model-View-ViewModel) cu separare clară a responsabilităților:

```

                    Views (XAML)                      
  LoginWindow · MainWindow · RegisterWindow           
  Pages: Dashboard · Products · Orders · Cart · ...  

                      DataBinding / Commands

                  ViewModels                          
  BaseViewModel · RelayCommand · NavigationService   
  LoginVM · MainVM · CartVM · AdminDashboardVM · ... 

                     

              Services & Auth                         
  AuthService · TokenService · DeviceAuthService     
  LanguageService · LoginPreferenceService           

                      Dapper

           Data / Repositories                        
  ProductRepository · OrderRepository · ...          

                      Microsoft.Data.SqlClient

              SQL Server 2022                         
  Users · Products · Orders · OrderDetails           
  UserDevices · OtpCodes                             

```

---

## Tehnologii

| Categorie | Tehnologie | Versiune |
|-----------|-----------|---------|
| Framework | .NET + WPF | 10.0 |
| ORM | Dapper | 2.1.72 |
| Bază de date | SQL Server | 2022 |
| Driver DB | Microsoft.Data.SqlClient | 7.0.1 |
| DI Container | Microsoft.Extensions.DependencyInjection | 10.0.7 |
| Config | System.Configuration.ConfigurationManager | 10.0.7 |
| Containerizare DB | Docker + docker-compose |  |

---

## Cerințe sistem

- **OS:** Windows 10 / 11 (x64)
- **.NET Runtime:** .NET 10
- **Bază de date:** SQL Server 2022 (local sau Docker)
- **RAM:** minim 512 MB
- **Spațiu disc:** ~50 MB

---

## Instalare și configurare

### Varianta 1  SQL Server local

1. **Clonează repository-ul:**
   ```bash
   git clone https://github.com/username/ShopManager.git
   cd ShopManager
   ```

2. **Configurează connection string-ul** în `Properties/App.config`:
   ```xml
   <connectionStrings>
     <add name="DefaultConnection"
          connectionString="Server=localhost;Database=ShopManager;
                            User Id=sa;Password=PAROLA_TA;
                            TrustServerCertificate=True;"
          providerName="System.Data.SqlClient"/>
   </connectionStrings>
   ```

3. **Compilează și rulează:**
   ```bash
   dotnet build
   dotnet run
   ```
   Schema bazei de date se creează **automat** la prima rulare.

---

### Varianta 2  Docker (recomandat)

1. **Pornește SQL Server în container:**
   ```bash
   docker-compose up -d
   ```
   SQL Server va fi disponibil pe `localhost:1433`.

2. **Connection string-ul** este deja configurat pentru Docker:
   ```
   Server=localhost,1433;Database=ShopManager;
   User Id=sa;Password=Practica2026!;TrustServerCertificate=True;
   ```

3. **Rulează aplicația:**
   ```bash
   dotnet run
   ```

---

## Structura proiectului

```
ShopManager/
 Auth/
    AuthService.cs          # Autentificare, hashing PBKDF2
    TokenService.cs         # Generare token RSA-2048
    DeviceAuthService.cs    # Amprentă dispozitiv SHA-256
    RoleManagementService.cs
    SessionStore.cs         # Persistare stare sesiune
 Config/
    DatabaseConfig.cs       # Conexiune SQL Server, inițializare schemă
 Converters/                  # IValueConverter pentru XAML
 Data/Repositories/           # Acces date cu Dapper
 Models/                      # User, Product, Order, CartItem, AuthToken
 Resources/
    Languages/
        ro-RO.xaml          # Texte română
        en-US.xaml          # Texte engleză
 Services/
    LanguageService.cs      # Schimbare limbă în timp real
    LoginPreferenceService.cs
 ViewModels/                  # MVVM ViewModels
 Views/
    Pages/                   # 8 pagini XAML
    LoginWindow.xaml
    MainWindow.xaml
    RegisterWindow.xaml
 docker-compose.yml
 Wpf.csproj
```

---

## Roluri utilizatori

| Rol | Acces |
|-----|-------|
| **Administrator** | Acces complet: produse, utilizatori, comenzi, setări, dashboard. Autentificare mereu manuală. |
| **Employee** | Gestiune produse, vizualizare comenzi, dashboard. Auto-login disponibil. |
| **Client** | Catalog, coș, plasare comenzi, vizualizare comenzi proprii. Auto-login disponibil. |

> **Cont implicit administrator** creat la prima rulare:
> - Username: `admin`
> - Parolă: `Admin123!`

---

## Securitate

### Hashing parole
```
PBKDF2-HMAC-SHA512 · 310.000 iterații · sare de 32 octeți (RandomNumberGenerator)
Comparare prin CryptographicOperations.FixedTimeEquals (anti timing-attack)
```

### Token sesiune
```
RSA-2048 · SHA-256 · PKCS#1 v1.5 · valabilitate 8 ore
Payload: { sub, userId, role, iat, exp }  Base64Url + semnătură
```

### Amprentă dispozitiv
```
SHA-256( MachineName | UserName | GUID_local )
GUID persistent în: %LOCALAPPDATA%\ProductManager\device.id
Stocat în tabela UserDevices cu IsActive flag
```

---

## Licență

Proiect academic  Final Project. Utilizare exclusiv în scop educațional.
