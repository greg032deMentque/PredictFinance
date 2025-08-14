# pipeline_main.py
import structlog
from fetchers.data_fetcher import fetch_data
from pipeline.pipeline_features  import build_features_and_labels
from pipeline.pipeline_dataset   import prepare_datasets
from pipeline.pipeline_training  import train_classification, train_regression
from pipeline.pipeline_config    import TICKER, START_DATE, END_DATE, RESULTS_DIR


log = structlog.get_logger("myapp.pipeline")


def main():
    """
    Orchestration du pipeline complet : récupération, feature engineering, entraînements.
    """
    log.info("Pipeline started", ticker=TICKER, start=START_DATE, end=END_DATE)
    print(f"Fetching data for {TICKER} from {START_DATE} to {END_DATE}…")
    df = fetch_data(TICKER, START_DATE, END_DATE)
    print("Building features and labels…")
    data = build_features_and_labels(df)

    print("Training classification models…")
    X_train, X_test, y_train, y_test = prepare_datasets(data, predict="classification")
    train_classification(X_train, X_test, y_train, y_test)

    print("Training regression models…")
    X_train, X_test, y_train, y_test = prepare_datasets(data, predict="regression")
    train_regression(X_train, X_test, y_train, y_test)

    print(f"Models saved in {RESULTS_DIR}/")
    log.info("Pipeline finished successfully")


if __name__ == "__main__":
    main()