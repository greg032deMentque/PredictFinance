# logging_config.py
import logging
import os
from logging.handlers import TimedRotatingFileHandler
from pathlib import Path

import structlog
from structlog.stdlib import ProcessorFormatter

def _build_timed_handler(base_path: Path, level: int) -> TimedRotatingFileHandler:
    """
    Crée un handler avec rotation quotidienne:
    - écrit d'abord dans base_path (ex: Logs/app_all.log)
    - à minuit: renomme en app_all_YYYY-MM-DD.log
    - conserve 30 fichiers (30 jours)
    """
    # On capture l'extension ".log" et le "basename" sans extension,
    # car après rotation, default_name ressemble à "app_all.log.2025-08-15"
    base_name_no_ext = base_path.with_suffix("").name   # "app_all"
    base_ext = base_path.suffix                         # ".log"

    h = TimedRotatingFileHandler(
        filename=str(base_path),
        when="midnight",
        interval=1,
        backupCount=30,
        encoding="utf-8",
        utc=False,
    )
    h.setLevel(level)
    h.suffix = "%Y-%m-%d"  # suffix utilisé par le handler (ex: ".2025-08-15")

    def namer(default_name: str) -> str:
        """
        Transforme ".../app_all.log.2025-08-15" en ".../app_all_2025-08-15.log"
        """
        p = Path(default_name)
        # On récupère la date après le dernier point
        date_part = p.name.split(".")[-1]  # "2025-08-15"
        return str(p.with_name(f"{base_name_no_ext}_{date_part}{base_ext}"))

    h.namer = namer
    return h


def setup_logging(level: int = logging.INFO) -> None:
    """
    Console lisible pour humain + 2 fichiers JSON (tous / erreurs) avec rotation quotidienne.
    Le répertoire est fixé à 'Logs' pour simplifier (modifiable ici si besoin).
    """
    # 1) Dossier des logs
    log_dir = Path("Logs")
    log_dir.mkdir(parents=True, exist_ok=True)

    # 2) Handlers fichiers
    file_all = _build_timed_handler(log_dir / "app_all.log", level=level)           # tous niveaux
    file_errors = _build_timed_handler(log_dir / "app_errors.log", level=logging.ERROR)  # erreurs seulement

    # 3) Console (affichage humain)
    console = logging.StreamHandler()
    console.setLevel(level)

    # 4) Formatters
    # Console: rendu lisible
    human_formatter = ProcessorFormatter(
        processors=[
            ProcessorFormatter.remove_processors_meta,
            structlog.dev.ConsoleRenderer(colors=True),
        ],
        foreign_pre_chain=[
            structlog.stdlib.filter_by_level,
            structlog.stdlib.add_log_level,
            structlog.processors.TimeStamper(fmt="iso", utc=False),
        ],
    )
    # Fichiers: JSON (machine-friendly)
    json_formatter = ProcessorFormatter(
        processors=[
            ProcessorFormatter.remove_processors_meta,
            structlog.processors.JSONRenderer(),
        ],
        foreign_pre_chain=[
            structlog.stdlib.filter_by_level,
            structlog.stdlib.add_log_level,
            structlog.processors.TimeStamper(fmt="iso", utc=False),
        ],
    )

    console.setFormatter(human_formatter)
    file_all.setFormatter(json_formatter)
    file_errors.setFormatter(json_formatter)

    # 5) Root logger
    root = logging.getLogger()
    root.handlers.clear()
    root.setLevel(level)
    root.addHandler(console)
    root.addHandler(file_all)
    root.addHandler(file_errors)

    # 6) structlog: processors côté "logger"
    structlog.configure(
        processors=[
            structlog.contextvars.merge_contextvars,
            structlog.processors.StackInfoRenderer(),
            structlog.processors.format_exc_info,
            structlog.stdlib.ProcessorFormatter.wrap_for_formatter,
        ],
        logger_factory=structlog.stdlib.LoggerFactory(),
        wrapper_class=structlog.make_filtering_bound_logger(level),
        cache_logger_on_first_use=True,
    )

    # 7) (optionnel) réduire le bruit de libs verbeuses
    logging.getLogger("urllib3").setLevel(logging.WARNING)
    logging.getLogger("mlflow").setLevel(logging.WARNING)

