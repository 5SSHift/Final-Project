# ShopManager — Ghid CI/CD cu GitHub Actions

## Ce face pipeline-ul?

```
git push → GitHub Actions pornește automat:

  1. 🔨 BUILD      — dotnet restore + dotnet build --Release
        ↓
  2. 🧪 TESTE      — dotnet test (cu SQL Server în container)
        ↓ (doar pe branch main)
  3. 📦 PUBLISH    — dotnet publish win-x64 + ZIP artifact
```

La **tag** (`v1.0.0`):
```
  🚀 RELEASE — build + publish + GitHub Release cu ZIP atașat automat
```

---

## Pași de configurare

### 1. Creează repository GitHub
```bash
git init
git add .
git commit -m "initial commit"
git branch -M main
git remote add origin https://github.com/USERNAME/ShopManager.git
git push -u origin main
```

### 2. Workflows sunt gata
Fișierele `.github/workflows/ci.yml` și `release.yml` sunt deja configurate.
La primul `git push`, GitHub Actions pornește automat.

### 3. Verifică pipeline-ul
1. Intră pe `https://github.com/USERNAME/ShopManager`
2. Click pe tab-ul **Actions**
3. Vei vedea workflow-ul rulând cu 3 job-uri: Build → Test → Publish

### 4. Creare Release versiune nouă
```bash
git tag v1.0.0
git push origin v1.0.0
```
GitHub creează automat un Release cu ZIP-ul atașat.

---

## Structura fișierelor adăugate

```
.github/
  workflows/
    ci.yml        ← Build + Test + Publish (la orice push)
    release.yml   ← GitHub Release automat (la tag v*.*.*)
```

---

## Badge status (adaugă în README)

```markdown
![CI](https://github.com/USERNAME/ShopManager/actions/workflows/ci.yml/badge.svg)
```
