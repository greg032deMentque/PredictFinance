# 09_CONTEXT_RELOAD_PROTOCOL

## Objectif

Ce document définit la méthode obligatoire de reprise du travail entre conversations pour éviter :
- la perte de contexte,
- la dérive hors corpus contractuel,
- le mélange entre `ARCHITECTURE_ARBITRATION` et `IMPLEMENTATION`,
- la réouverture d’arbitrages déjà `LOCKED`,
- une implémentation prématurée avant le switch contractuel.

Ce document est contractuel pour la continuité du travail.

---

## Règle absolue

Chaque nouveau message exécuté dans la séquence d’arbitrage doit mettre à jour :
- ce document si la méthode évolue,
- et surtout `Docs/CHECKPOINT_CURRENT.md`.

Aucune nouvelle conversation ne doit repartir de la mémoire supposée du chat seul.

La reprise doit toujours s’appuyer sur des fichiers écrits dans le projet.

---

## Sources de vérité pour la reprise

Ordre de priorité :

1. `predictfinance_checkpoint_resume_message30.md`
2. `refont_by_bloc.txt`
3. `refonte_plan.txt`
4. `Docs/CHECKPOINT_CURRENT.md`
5. `Docs/09_CONTEXT_RELOAD_PROTOCOL.md`
6. les derniers retours validés de la conversation
7. le code réel

En cas de conflit :
- la source la plus prioritaire gagne,
- le conflit doit être signalé explicitement,
- aucune réponse hybride ne doit lisser le conflit.

---

## Mode de travail

Deux modes sont mutuellement exclusifs :

- `ARCHITECTURE_ARBITRATION`
- `IMPLEMENTATION`

Tant que le corpus contractuel n’active pas explicitement `IMPLEMENTATION`, le travail doit rester en `ARCHITECTURE_ARBITRATION`.

Aucune implémentation réelle ne doit être lancée tant que le switch contractuel n’est pas atteint.

---

## Point de bascule connu

Le corpus actuel impose :
- arbitrage jusqu’au `Message 80` inclus,
- switch d’implémentation au `Message 81`.

Conséquence :
- avant `Message 81`, pas de code, pas de pseudo-code, pas de repo final, pas de zip final,
- à partir du `Message 81`, implémentation autorisée uniquement si le corpus ne contient plus de blocage supérieur.

---

## Obligation de checkpoint

Après chaque message exécuté :
1. marquer le message comme terminé,
2. lister les blocs exécutés,
3. marquer chaque bloc `LOCKED` si le retour le confirme,
4. calculer le prochain message,
5. calculer les prochains blocs,
6. maintenir l’état du `Hard stop`,
7. maintenir l’état `Implementation allowed now`,
8. mettre à jour le bloc de mise en contexte,
9. sauvegarder le tout dans `Docs/CHECKPOINT_CURRENT.md`.

---

## Bloc de mise en contexte obligatoire

Chaque nouveau message ou nouvelle conversation doit repartir avec un bloc de mise en contexte mis à jour.

Ce bloc doit être présent dans `Docs/CHECKPOINT_CURRENT.md` et doit être copié dans toute nouvelle conversation de reprise.

Template obligatoire :

## Context reload block

### Contract mode
[ARCHITECTURE_ARBITRATION | IMPLEMENTATION]

### Current checkpoint
[Last completed prompt message: ...]
[Next message to execute: ...]
[Next blocks to execute: ...]

### Implementation allowed now
[YES | NO]

### Hard stop status
[ACTIVE | INACTIVE]

### Locked blocks summary
- ...
- ...
- ...

### Binding constraints summary
- ...
- ...
- ...

### Immediate instruction
Execute only the next authorized message and stop exactly at scope boundary.

Règles :
- ce bloc doit être réécrit après chaque nouveau message exécuté,
- il doit refléter l’état courant exact,
- il ne doit pas contenir d’hypothèse libre,
- il ne doit pas annoncer des blocs futurs au-delà du prochain message utile,
- il ne doit pas dériver vers une roadmap d’implémentation si le mode reste `ARCHITECTURE_ARBITRATION`.

---

## Préambule obligatoire dans chaque conversation de reprise

Toute nouvelle conversation de reprise doit commencer par la lecture des fichiers contractuels et par la production de ce préambule exact :

## Contract mode
[ARCHITECTURE_ARBITRATION | IMPLEMENTATION]

## Resume point
[Last completed prompt message: ...]
[Next message to execute: ...]
[Next blocks to execute: ...]

## Implementation allowed now
[YES | NO]

## Conflict check
[NO CONFLICT | CONFLICT DETECTED]

## Binding source
[checkpoint | block charter | plan | codebase]

## Hard stop status
[ACTIVE | INACTIVE]

Aucun travail ne doit commencer avant ce préambule.

---

## Règle de non-réouverture

Un message terminé et ses blocs `LOCKED` ne doivent pas être rouverts sauf si :
- un fichier contractuel de priorité supérieure l’impose explicitement,
- ou un conflit démontré invalide matériellement le lock.

Sinon :
- pas de réinterprétation,
- pas d’assouplissement,
- pas de reformulation adoucissante,
- pas de redesign opportuniste.

---

## Règle de non-anticipation

Tant que le mode reste `ARCHITECTURE_ARBITRATION`, il est interdit de :
- produire du code,
- produire du pseudo-code,
- produire une arborescence de repo finale,
- préparer un zip final,
- détailler une migration technique exécutable,
- sauter plusieurs messages,
- citer des blocs futurs comme justification opérationnelle,
- transformer l’arbitrage en plan d’implémentation.

---

## Politique de découpage des conversations

Pour éviter la perte de mémoire :
- ne pas laisser la conversation devenir la seule source de contexte,
- créer un point de reprise écrit après chaque message,
- créer un rechargement consolidé tous les 5 à 10 messages,
- ouvrir une nouvelle conversation quand le contexte devient lourd.

Bundle minimal conseillé pour redémarrer une nouvelle conversation :
- `refont_by_bloc.txt`
- `refonte_plan.txt`
- le dernier checkpoint source,
- `Docs/CHECKPOINT_CURRENT.md`
- `Docs/09_CONTEXT_RELOAD_PROTOCOL.md`
- les 1 à 3 derniers retours validés.

---

## Fréquence recommandée de consolidation

### Après chaque message
Mettre à jour `Docs/CHECKPOINT_CURRENT.md`.

### Tous les 5 messages
Créer ou rafraîchir un résumé consolidé contenant :
- messages terminés,
- blocs `LOCKED`,
- invariants actifs,
- contradictions observées,
- prochain message à exécuter.

### À chaque changement de conversation
Copier le bloc de mise en contexte dans le premier prompt.

---

## Format recommandé pour `Docs/CHECKPOINT_CURRENT.md`

Template :

# CHECKPOINT_CURRENT

## Contract mode
[ARCHITECTURE_ARBITRATION | IMPLEMENTATION]

## Resume point
[Last completed prompt message: ...]
[Next message to execute: ...]
[Next blocks to execute: ...]

## Implementation allowed now
[YES | NO]

## Conflict check
[NO CONFLICT | CONFLICT DETECTED]

## Binding source
[checkpoint | block charter | plan | codebase]

## Hard stop status
[ACTIVE | INACTIVE]

## Last completed blocks
- [Block ID] - [Title] - [Closure]
- [Block ID] - [Title] - [Closure]

## Locked blocks cumulative summary
- ...
- ...
- ...

## Active non-negotiable constraints
- ...
- ...
- ...

## Known contradictions accepted but not reopening locks
- ...
- ...
- ...

## Context reload block

### Contract mode
[ARCHITECTURE_ARBITRATION | IMPLEMENTATION]

### Current checkpoint
[Last completed prompt message: ...]
[Next message to execute: ...]
[Next blocks to execute: ...]

### Implementation allowed now
[YES | NO]

### Hard stop status
[ACTIVE | INACTIVE]

### Locked blocks summary
- ...
- ...
- ...

### Binding constraints summary
- ...
- ...
- ...

### Immediate instruction
Execute only the next authorized message and stop exactly at scope boundary.

---

## Seed initial recommandé avec l’état courant

État connu à la rédaction de ce document :
- `Message 30` terminé,
- `P04C-B004` = `LOCKED`,
- `P04C-B005` = `LOCKED`,
- `Message 31` terminé,
- `P04C-B006` = `LOCKED`,
- `P04C-B007` = `LOCKED`,
- `Message 32` terminé,
- `P05-B001` = `LOCKED`,
- `P05-B002` = `LOCKED`,
- prochain message attendu : `Message 33`,
- mode courant : `ARCHITECTURE_ARBITRATION`,
- `Implementation allowed now = NO`,
- `Hard stop status = ACTIVE`.

---

## Règle d’entretien

À chaque nouveau message :
- remplacer `Last completed prompt message`,
- remplacer `Next message to execute`,
- remplacer `Next blocks to execute`,
- ajouter les nouveaux blocs `LOCKED`,
- mettre à jour le `Context reload block`,
- ne jamais laisser ce document en retard d’un message.

Ce document doit toujours représenter l’état de reprise le plus récent.