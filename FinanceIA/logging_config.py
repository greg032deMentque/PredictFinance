# logging_config.py
import logging
from pathlib import Path
from logging.handlers import TimedRotatingFileHandler

import structlog
from structlog.stdlib import ProcessorFormatter

# ⚠️ Mets ici ton chemin absolu (local ou serveur)
LOG_DIR = Path(r"C:\Users\gregd\Documents\greg\Projet perso\GitHub-projects\PredictFinance\FinanceIA\Logs")
# LOG_DIR = Path("/var/log/financeia")   # <- décommente ça sur ton serveur Linux

def _build_timed_handler(base_path: Path, level: int) -> TimedRotatingFileHandler:
    base_name_no_ext = base_path.with_suffix("").name
    base_ext = base_path.suffix
    h = TimedRotatingFileHandler(
        filename=str(base_path),
        when="midnight",
        interval=1,
        backupCount=30,
        encoding="utf-8",
        utc=False,
    )
    h.setLevel(level)
    h.suffix = "%Y-%m-%d"

    def namer(default_name: str) -> str:
        p = Path(default_name)
        date_part = p.name.split(".")[-1]
        return str(p.with_name(f"{base_name_no_ext}_{date_part}{base_ext}"))

    h.namer = namer
    return h


def setup_logging(level: int = logging.INFO) -> None:
    """
    Configure le logging avec rotation quotidienne.
    Les fichiers sont toujours créés dans LOG_DIR (chemin absolu).
    """
    LOG_DIR.mkdir(parents=True, exist_ok=True)

    file_all = _build_timed_handler(LOG_DIR / "app_all.log", level=level)
    file_errors = _build_timed_handler(LOG_DIR / "app_errors.log", level=logging.ERROR)

    console = logging.StreamHandler()
    console.setLevel(level)

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

    root = logging.getLogger()
    root.handlers.clear()
    root.setLevel(level)
    root.addHandler(console)
    root.addHandler(file_all)
    root.addHandler(file_errors)

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

    logging.getLogger("urllib3").setLevel(logging.WARNING)
    logging.getLogger("mlflow").setLevel(logging.WARNING)
