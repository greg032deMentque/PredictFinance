#!/usr/bin/env python3
import subprocess
import sys
import os
from pathlib import Path

# Chemin racine du projet
BASE_DIR = Path(__file__).resolve().parent

# Activation du venv si nécessaire
VENV_PATH = BASE_DIR / "venv" / "bin" / "activate"
if VENV_PATH.exists():
    print(f"[INFO] Activation de l'environnement virtuel : {VENV_PATH}")
    activate_cmd = f"source {VENV_PATH}"
else:
    activate_cmd = ""

# Commande pytest
pytest_cmd = "pytest -v --maxfail=1 --disable-warnings"

# Exécution
full_cmd = f"{activate_cmd} && {pytest_cmd}" if activate_cmd else pytest_cmd
print(f"[INFO] Lancement des tests : {pytest_cmd}")

result = subprocess.run(full_cmd, shell=True)
sys.exit(result.returncode)
