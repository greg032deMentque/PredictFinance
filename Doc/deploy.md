# 🚀 Déploiement automatisé avec GitLab CI/CD vers un serveur distant

Ce guide documente l'installation et la configuration d'un pipeline GitLab CI/CD qui déploie automatiquement un dossier Python (`FinanceIA`) sur un serveur distant Ubuntu via SSH.

---

## 📁 Structure du dépôt

Le dépôt contient :
- Un dossier `FinanceIA` avec un projet Python
- Un script principal : `pipeline/pipeline_double_top/analyze_double_top.py`
- Un fichier `.gitlab-ci.yml` dans la racine

---

## ⚙️ Configuration de GitLab

### 1. 🔐 Variables à ajouter dans **Settings > CI/CD > Variables**

| Nom | Description | Type |
|-----|-------------|------|
| `DEPLOY_SERVER` | IP ou hostname de ton serveur distant | Masquée |
| `DEPLOY_USER` | Utilisateur SSH distant (ex: `ubuntu`) | Masquée |
| `SSH_PRIVATE_KEY_B64` | Clé privée SSH **format PEM**, encodée en base64 **(1 ligne)** | Masquée + Protégée ✅ |

### Génération d'une clé compatible (format PEM + base64)

```bash
ssh-keygen -t rsa -m PEM -b 4096 -f gitlab_deploy_key -N ""
base64 -w0 gitlab_deploy_key > gitlab_deploy_key.b64
cat gitlab_deploy_key.b64  # à copier dans SSH_PRIVATE_KEY_B64
cat gitlab_deploy_key.pub >> ~/.ssh/authorized_keys  # sur le serveur distant


🔐 Protéger la branche dev
Va dans Settings > Repository > Protected Branches, et protège la branche dev pour que les variables protégées soient injectées.



workflow:
  rules:
    - if: '$CI_COMMIT_BRANCH == "dev"'
      when: always

stages:
  - deploy

IA-staging:
  stage: deploy
  image: python:3.11
  environment:
    name: staging
  only:
    refs:
      - dev

  variables:
    VENV_PATH: FinanceIA/venv

  before_script:
    - mkdir -p ~/.ssh
    - echo "[CI] ➕ Décodage de la clé privée SSH"
    - echo "$SSH_PRIVATE_KEY_B64" | base64 -d > ~/.ssh/gitlab_deploy
    - chmod 600 ~/.ssh/gitlab_deploy

    - echo "[CI] 🚀 Démarrage de ssh-agent"
    - eval "$(ssh-agent -s)"
    - echo "[CI] 🔍 Vérification du format de la clé"
    - file ~/.ssh/gitlab_deploy
    - head -n 5 ~/.ssh/gitlab_deploy

    - echo "[CI] 🔐 Ajout de la clé à ssh-agent"
    - ssh-add ~/.ssh/gitlab_deploy || { echo "❌ Échec de l'ajout de la clé SSH"; exit 1; }

    - echo "[CI] 🧠 Ajout du serveur à known_hosts"
    - ssh-keyscan -H "$DEPLOY_SERVER" >> ~/.ssh/known_hosts 2>/dev/null
    - mkdir -p logs

  script:
    - echo "[CI] 🚚 Envoi du dossier FinanceIA sur le serveur distant"
    - scp -r FinanceIA "$DEPLOY_USER@$DEPLOY_SERVER:/var/www/predictFinance/FinanceIA"

    - echo "[CI] 🧠 Exécution du script distant sur le serveur"
    - |
      echo "
        set -e
        cd /var/www/predictFinance/FinanceIA
        python3 -m venv \"$VENV_PATH\"
        source \"$VENV_PATH/bin/activate\"
        pip install -r requirements.txt
        python pipeline/pipeline_double_top/analyze_double_top.py
      " | ssh "$DEPLOY_USER@$DEPLOY_SERVER" > logs/remote_execution.log 2>&1

    - |
      RESULT=$?
      if [ $RESULT -eq 0 ]; then
        echo "[CI] ✅ Script distant terminé avec succès"
      else
        echo "[CI] ❌ Échec du script distant avec code $RESULT"
        cat logs/remote_execution.log
        exit $RESULT
      fi

  artifacts:
    name: "ia-staging-logs"
    when: always
    paths:
      - logs/remote_execution.log
    expire_in: 2 days



🧪 Débogage
Si le pipeline échoue :

Consulte l’onglet Job > Artifacts > logs/remote_execution.log

Vérifie les erreurs Python, pip ou permissions

Tu peux ajouter dans before_script :

yaml
Copier
Modifier
- cat ~/.ssh/gitlab_deploy | head -n 5
- ssh -v "$DEPLOY_USER@$DEPLOY_SERVER"
