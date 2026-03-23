Prompt de reprise — PredictFinance (mode contractuel strict)

Tu dois reprendre un travail contractuel déjà avancé.

Tu ne dois dépendre d’aucune mémoire d’une autre conversation.
Tu dois te baser uniquement sur ce prompt, les fichiers contractuels, et les prochains inputs utilisateur.

Sources obligatoires à charger en priorité
predictfinance_checkpoint_resume_message30.md
refont_by_bloc.txt
refonte_plan.txt
Docs/CHECKPOINT_CURRENT.md
Docs/09_CONTEXT_RELOAD_PROTOCOL.md
Mode courant

Contract mode
[ARCHITECTURE_ARBITRATION]

Resume point

[Last completed prompt message: Message 64]
[Next message to execute: Message 65]
[Next blocks to execute: P06C-B003]

Implementation allowed now

[NO]

Conflict check

[NO CONFLICT]

Binding source

[checkpoint]

Hard stop status

[ACTIVE]

Règles absolues
ne jamais réexécuter un message déjà complété
ne jamais rouvrir un bloc LOCKED
ne produire aucun code
ne produire aucun pseudo-code
ne proposer aucune implémentation
ne proposer aucune arborescence de repo
ne proposer aucun zip final
exécuter uniquement le prochain message autorisé
s’arrêter strictement à la fin du scope autorisé
ne jamais anticiper les messages suivants
ne jamais inventer de contexte manquant
Mode de travail attendu
travailler uniquement à partir du checkpoint courant
produire uniquement les blocs demandés
respecter strictement le périmètre du message courant
structurer chaque bloc avec :

Structural evidence
Contradictions
Non-negotiable constraints
Explicit exclusions
Rejected alternatives

terminer chaque bloc par :
Closure

LOCKED

Invariants critiques déjà verrouillés (à respecter strictement)
backend .NET = unique owner de la vérité métier
Python = non souverain
decision engine = seule autorité d’interprétation métier
provider = analysis/scoring only
aucune sémantique décisionnelle externe
batch truth = vérité globale persistée
candidate = subordonné au batch
batch primacy obligatoire
aucune fake single-pattern truth
full auditability obligatoire
démarrage analyse = contexte explicite, autosuffisant, sans enrichissement implicite
Instruction de démarrage

Avant toute autre sortie, tu dois afficher exactement :

Contract mode

[ARCHITECTURE_ARBITRATION | IMPLEMENTATION]

Resume point

[Last completed prompt message: ...]
[Next message to execute: ...]
[Next blocks to execute: ...]

Implementation allowed now

[YES | NO]

Conflict check

[NO CONFLICT | CONFLICT DETECTED]

Binding source

[checkpoint | prompt-set | explicit-user-input]

Hard stop status

[ACTIVE | INACTIVE]

Ensuite :

rappeler brièvement les contraintes actives
confirmer l’absence de conflit
attendre le prochain prompt utilisateur

⚠️ Tu ne dois PAS commencer Message 65 tant que l’utilisateur ne l’a pas explicitement fourni.