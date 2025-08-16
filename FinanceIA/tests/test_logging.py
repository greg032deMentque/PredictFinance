# tests/test_logging.py
# ------------------------------------------------------------
# Test minimal : vérifie que FinanceIA/Logs/ est créé et
# que des fichiers de log y sont bien écrits.
# ------------------------------------------------------------
from pathlib import Path
import shutil
import time

import structlog
from logging_config import setup_logging  # import depuis la racine du projet

def test_logs_folder_created_and_files_written():
    # 1) Localise la racine du projet (dossier FinanceIA)
    project_root = Path(__file__).resolve().parents[1]
    log_dir = project_root / "Logs"

    # 2) Nettoyage pour un test propre
    if log_dir.exists():
        shutil.rmtree(log_dir)

    # 3) Initialise la config de logging -> doit créer Logs/
    setup_logging()

    # 4) Émet quelques logs
    logger = structlog.get_logger(__name__)
    logger.info("pytest_info", msg="log INFO via pytest")
    logger.error("pytest_error", msg="log ERROR via pytest")

    # 5) Petite latence pour laisser les handlers flusher
    time.sleep(0.05)

    # 6) Asserts
    assert log_dir.exists(), "Le dossier Logs/ n'a pas été créé."
    app_all = log_dir / "app_all.log"
    app_err = log_dir / "app_errors.log"
    assert app_all.exists(), "app_all.log n'existe pas."
    assert app_err.exists(), "app_errors.log n'existe pas."
    assert app_all.stat().st_size > 0, "app_all.log est vide."
    assert app_err.stat().st_size > 0, "app_errors.log est vide."
