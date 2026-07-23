# PredictFinance — Instructions agents

## Regle universelle : plan avant code

Pour **toute tache impliquant des changements de code**, sans exception et sans attendre que
l'utilisateur le demande :

**Phase 1 — Plan uniquement** (prefixer la reponse de `[PHASE 1 — PLAN]`)
- Identifier les fichiers touches et leur projet (`API`, `Services`, `Datas`, `ViewModels`, etc.)
- Lister les changements prevus fichier par fichier
- Classifier selon AGENTS.md : PROVEN / DECIDED / PROPOSED / DEROGATION / REMAINING TO ARBITRATE
- Indiquer si des tests back sont necessaires (voir regle tests)
- Challenger le plan de maniere rigoureuse avant de le presenter : cas limites, effets de bord, consequences sur le reste du code. Le plan doit avoir le meme niveau de detail qu'un plan produit en Plan Mode. Verifier l'alignement avec l'architecture definie dans les agents `dotnet-api-architect`/`angular-architect` (`C:\Users\gregd\.claude\agents`).
- S'arreter. Attendre la confirmation explicite de l'utilisateur avant toute generation.

**Phase 2 — Execution** (prefixer de `[PHASE 2 — EXECUTION]`, uniquement apres "ok", "go", "continue")
- Generer le code depuis le plan valide
- Emettre une ligne de progression a chaque etape majeure :
  `[1/N] Services...`, `[2/N] ViewModels...`, etc.

**Ne jamais commencer a ecrire du code sans avoir presente et fait valider le plan.**
L'agent decompose de lui-meme — l'utilisateur ne doit pas le demander.

**Exceptions — repondre directement sans Phase 1 :**
- Mise a jour ou correction du plan deja presente dans la conversation
  ("ajoute X au plan", "retire Y", "mets a jour le plan")
  → Modifier le plan directement dans la reponse. Ne pas aller chercher dans le code.
- Question sur le plan en cours ("pourquoi ce fichier ?", "c'est quoi l'impact ?")
  → Repondre depuis le contexte de la conversation uniquement.
- Tache purement conversationnelle, explicative ou de lecture seule.

---

## Regle tests

**Tests frontend (FinanceFront) : ne pas creer.**
Ne pas generer de tests Angular/Jasmine, ne pas les suggerer.

**Tests backend (BackPredictFinance.Tests) : creer uniquement si la logique est absolument essentielle.**
Un test est justifie si et seulement si :
- il prouve un comportement metier a risque de regression (detection de pattern, scoring, regles recommendation, cas negatif critique)
- l'absence du test laisserait passer une regression silencieuse sans aucun signal

Un test n'est PAS justifie si :
- il teste du plumbing sans logique (getter/setter, mapping trivial, controleurs thin)
- il existe uniquement pour satisfaire une couverture de code

En cas de doute : ne pas creer le test.

---

## Routage agents

### Feature full-stack (back + front) ou perimetre incertain
→ `project-orchestrator`

### Backend seul
→ `dotnet-api-architect`
Fournir uniquement : description du changement + section concernee de `AGENTS.md`.
Ne pas pre-charger tous les fichiers — laisser l'architecte demander ce dont il a besoin.

### Frontend seul
→ `angular-architect`

### Revue / conformite / gap analysis / audit (lecture seule)
→ Traiter directement, sans deleguer a `review-validator`.
`review-validator` n'apporte rien quand il n'y a pas de corrections a router.

### Apres implementation (corrections a router)
→ `review-validator`
Un seul perimetre a la fois. Passer le contrat API + les fichiers touches.
Ne pas pre-collecter d'autres fichiers.

Si j'ai besoin de contexte supplementaire : une seule question courte.

---

## Architecture

```
FinanceBack/
  BackPredictFinance.API        -> HTTP delivery uniquement (controllers thin)
  BackPredictFinance.Services   -> logique metier, organise par capability
    Analysis/Application        -> orchestration
    Analysis/Patterns           -> detection
    Analysis/Scoring            -> scoring
    Analysis/Risk               -> risque
    Analysis/Advice             -> recommendation
    Analysis/Persistence        -> persistance
    Analysis/History            -> historique
    Fundamentals                -> scoring fondamental / PEA
  BackPredictFinance.Datas      -> EF entities, DbContext, migrations
  BackPredictFinance.ViewModels -> DTOs request/response, profils AutoMapper
  BackPredictFinance.Common     -> partage cross-project (garder minimal)
  BackPredictFinance.Patterns   -> definitions de patterns deterministes
  BackPredictFinance.Tests      -> preuves de comportement (voir regle tests)
FinanceFront/                   -> Angular, rendering layer uniquement
Doc/                            -> contrats canoniques (source de verite)
AGENTS.md                       -> contrat de travail complet — lire en premier
```

## Conventions cles

- `BaseService<T>` : utiliser uniquement si reduction reelle de duplication
- Soft-delete : `IsDeleted`, jamais de DELETE SQL
- Pagination : `PagedListViewModel<T>` obligatoire sur les listes
- Nouveau projet : interdit par defaut (voir AGENTS.md)
- Frontend = rendering layer, jamais de verite metier
- Nommage : anglais dans le code, francais pour les textes utilisateur

## Specs canoniques

- Contrat de travail complet : `AGENTS.md`
- Documentation produit : `Doc/v1/`
- Carte canonique : `Doc/v1/00_INDEX.md`
- Backlog sprint V1 : `Doc/PredictFinance_V1_Sprint_Backlog_Operational.md`
