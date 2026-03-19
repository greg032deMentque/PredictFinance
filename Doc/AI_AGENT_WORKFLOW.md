# PredictFinance - AI Agent Workflow

## 1. Role du document

Ce document decrit la methode attendue pour toute intervention par un agent IA dans ce repo.

Il sert a:

- comprendre comment intervenir sans casser l'architecture
- imposer un ordre de lecture avant toute modification
- rappeler les regles de granularite, de validation et de mise a jour documentaire

## 2. Ordre de lecture obligatoire avant toute modification

Ordre de lecture impose:

1. `README.md`
2. `PRODUCT_ARCHITECTURE.md`
3. `IDEAS.md`
4. le code reel concerne
5. seulement ensuite proposition de modification

Regle:

- ne pas commencer a coder sans avoir lu ces documents dans cet ordre
- si un document est obsolete ou incomplet, le signaler et le mettre a jour si le lot le justifie

## 3. Principes generaux de travail

- partir du code existant
- ne pas faire de refonte big bang
- stabiliser avant d'etendre
- travailler par petites etapes
- ne pas melanger trop de sujets dans un meme lot
- preserver les contrats existants tant qu'ils ne sont pas officiellement changes
- expliciter les dependances
- signaler les risques
- mettre a jour la documentation quand un changement structurant est introduit

Principe fort:

- si un sujet est priorise apres autre chose dans `IDEAS.md`, ne pas le remonter artificiellement sans justification explicite

## 4. Regles d'architecture a respecter

- Angular ne porte pas la logique metier critique
- .NET orchestre, valide, securise et historise
- Python IA reste centre sur les patterns
- les modules non-IA restent deterministes
- eviter la duplication de logique entre .NET et Python
- eviter le hardcode de comportements metier dans le front
- ne pas faire dependre portefeuille, exposition, risque, news et alertes du moteur IA

Applications concretes:

- pas de recalcul pattern/recommandation dans Angular
- pas de duplication de logique de phase entre .NET et Python
- pas d'utilisation de l'IA pour calculer PRU, P/L, exposition ou score de risque global

## 5. Methode de correction attendue

Tout correctif doit suivre une sequence simple.

### Analyse

- relire les documents
- relire les fichiers concernes
- reformuler le probleme

### Perimetre

- definir un lot petit et coherent
- citer les fichiers touches
- preciser ce qu'il ne faut pas casser

### Risque

- identifier les regressions possibles
- noter les dependances

### Validation attendue

- build
- tests
- typecheck
- verifications fonctionnelles minimales

### Execution

- modifier uniquement le perimetre annonce

### Verification

- relancer la validation prevue
- verifier la coherence avec les documents

### Mise a jour documentation

- mettre a jour les bons documents si le changement est structurant

## 6. Methode d'ajout de fonctionnalite

Avant d'ajouter une fonctionnalite, verifier:

- sa priorite dans `IDEAS.md`
- sa coherence avec `PRODUCT_ARCHITECTURE.md`
- sa compatibilite avec le `README.md`
- ses dependances techniques
- si elle est IA ou non-IA
- si elle est proche ou non de l'existant

Regle:

- si la fonctionnalite est loin du code actuel et de priorite basse, ne pas la traiter avant les sujets de stabilisation ou de valeur proche

## 7. Granularite attendue des travaux

Changements attendus:

- petits lots coherents
- lisibles
- testables
- reversibles

Exemples de bons lots:

- corriger un endpoint
- stabiliser un DTO
- ajouter une validation
- brancher un middleware
- enrichir un composant cible
- ajouter une persistance ciblee
- mettre a jour un contrat

Exemples de mauvais lots:

- refondre tout le systeme
- reecrire toute l'auth
- faire du multi-pattern partout d'un coup
- modifier front + back + python massivement dans un seul lot

## 8. Gestion de la documentation

Mettre a jour:

- `README.md` si la technique, l'architecture reelle ou les contrats changent
- `PRODUCT_ARCHITECTURE.md` si la structure systeme ou le role des domaines evolue
- `IDEAS.md` si la priorisation ou la vision produit change
- `AI_AGENT_WORKFLOW.md` si la methode de travail change

Regle:

- ne pas documenter seulement apres coup
- si le lot change la realite du repo, la documentation doit changer dans la meme intervention

## 9. Checklists utiles

### Checklist avant modification

- ai-je lu `README.md` ?
- ai-je lu `PRODUCT_ARCHITECTURE.md` ?
- ai-je lu `IDEAS.md` ?
- ai-je lu les fichiers reels concernes ?
- ai-je delimite un petit perimetre ?

### Checklist avant fusion logique

- le lot est-il petit et coherent ?
- les dependances sont-elles explicites ?
- les contrats existants sont-ils preserves ou officiellement modifies ?
- ai-je evite un changement lateral inutile ?

### Checklist securite

- auth et roles impactes ?
- validation serveur suffisante ?
- erreurs non sensibles ?
- CORS / rate limiting / middleware impactes ?
- aucune fuite de donnees sensibles ?

### Checklist documentation

- le `README.md` doit-il changer ?
- `PRODUCT_ARCHITECTURE.md` doit-il changer ?
- `IDEAS.md` doit-il changer ?
- la methode de travail a-t-elle change ?

### Checklist fin de lot

- build/tests/typecheck faits ?
- README et autres docs alignes ?
- risques restants notes ?
- prochaine etape logique identifiee ?

## 10. Anti-patterns a eviter

- documenter apres coup seulement
- coder sans avoir lu les documents
- ajouter une fonctionnalite hors priorite sans justification
- creer de la duplication de logique
- faire dependre portefeuille/exposition du moteur IA
- introduire une complexite abstraite inutile
- modifier plusieurs couches sans plan clair
- lancer une refonte globale alors qu'un micro-lot suffit

## 11. Format de travail recommande pour agents IA

Format simple reutilisable:

Analyse:

- que corrige ou ajoute le lot

Hypothese:

- quelles hypotheses raisonnables sont prises

Perimetre:

- fichiers touches
- limites du lot

Risques:

- regressions et points sensibles

Validation:

- build/tests/typecheck/verifications fonctionnelles

Documentation a mettre a jour:

- README / PRODUCT_ARCHITECTURE / IDEAS / AI_AGENT_WORKFLOW

## 12. Regle finale de discipline

- commencer petit
- verifier souvent
- documenter quand la realite change
- ne pas faire de l'IA la solution a tout
- traiter d'abord les parcours, la securite et la stabilite avant les extensions ambitieuses
