import argparse
import structlog
from logging_config import setup_logging
import pipeline.pipeline_config as config
from pipeline.pipeline_main import main as run_pipeline


def main():
    """
    Point d'entrée pour lancer l'ensemble du pipeline : récupération des données Yahoo Finance,
    feature engineering et entraînement des modèles pour un ou plusieurs tickers spécifiés.
    exemple : python main.py --tickers AAPL MSFT GOOGL ou python main.py (par défault AAPL)
    """
    parser = argparse.ArgumentParser(description="Lance le pipeline pour un ou plusieurs tickers boursiers")
    parser.add_argument(
        "-t", "--tickers",
        nargs="+",
        default=[config.TICKER],
        help=f"Liste des tickers (par défaut: {config.TICKER})"
    )
    args = parser.parse_args()

    # Initialisation du logging
    setup_logging()
    logger = structlog.get_logger("myapp")
    logger.info("Démarrage du pipeline", tickers=args.tickers)

    for ticker in args.tickers:
        logger.info("Traitement du ticker", ticker=ticker)
        # Override dynamique du ticker dans la config
        config.TICKER = ticker
        run_pipeline()


if __name__ == "__main__":
    main()
