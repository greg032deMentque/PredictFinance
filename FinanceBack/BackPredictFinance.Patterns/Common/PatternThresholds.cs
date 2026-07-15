namespace BackPredictFinance.Patterns.Common
{
    /// <summary>
    /// Centralise les seuils de detection des patterns de continuation.
    ///
    /// Principe : les seuils sensibles a la volatilite sont exprimes en multiples d'ATR
    /// (Average True Range). Un seuil de breakout en pourcentage fixe (ex. +0,3 %) n'a pas le
    /// meme sens sur une valeur calme et sur une valeur volatile ; le ramener a une fraction de
    /// l'amplitude quotidienne typique (ATR) lui redonne une signification homogene entre actifs.
    ///
    /// Les seuils de forme (ratios sans dimension : gain du pole, hauteur relative du flag, etc.)
    /// restent en valeur brute car ils decrivent une geometrie, pas une distance de prix.
    ///
    /// Valeurs de depart raisonnees, destinees a etre calibrees a posteriori via les KPI
    /// d'evaluation ex post (taux de cible atteinte / invalidation par version de moteur).
    /// </summary>
    internal static class PatternThresholds
    {
        // --- Seuils normalises par la volatilite (multiples d'ATR) ---

        /// <summary>
        /// Distance minimale au-dela d'une borne, en multiples d'ATR, pour qualifier un breakout.
        /// 0,25xATR ~ un quart de l'amplitude quotidienne typique : filtre le bruit sans exiger un
        /// mouvement extreme.
        /// </summary>
        public const decimal BreakoutAtrMultiple = 0.25m;

        /// <summary>
        /// Pente maximale (en ATR par bougie) toleree pour considerer la consolidation d'un flag
        /// comme suffisamment plate / contre-tendance ordonnee.
        /// </summary>
        public const decimal FlagMaxSlopeAtrPerCandle = 0.10m;

        /// <summary>
        /// Pente maximale (en ATR par bougie) toleree pour les bornes d'un rectangle, qui doivent
        /// rester quasi horizontales.
        /// </summary>
        public const decimal RectangleMaxBoundarySlopeAtrPerCandle = 0.10m;

        /// <summary>
        /// Tolerance (en multiples d'ATR) pour considerer qu'une bougie "touche" une borne du
        /// rectangle.
        /// </summary>
        public const decimal RectangleTouchToleranceAtrMultiple = 0.30m;

        /// <summary>
        /// Amplitude minimale (en multiples d'ATR) du mouvement prealable pour qualifier une
        /// tendance directionnelle exploitable comme contexte de continuation.
        /// </summary>
        public const decimal PriorTrendMinMoveAtrMultiple = 1.5m;

        // --- Seuils de forme (ratios sans dimension) ---

        /// <summary>Gain (ou perte) minimal du pole d'un flag : l'impulsion initiale doit etre nette.</summary>
        public const decimal FlagMinPoleMovePct = 0.08m;

        /// <summary>Seuil d'impulsion forte du pole, bonifie dans le score de confiance.</summary>
        public const decimal FlagStrongPoleMovePct = 0.12m;

        /// <summary>Hauteur maximale du flag rapportee a sa cloture moyenne (consolidation courte).</summary>
        public const decimal FlagMaxHeightRatio = 0.18m;

        /// <summary>Retracement maximal du flag rapporte a la hauteur du pole.</summary>
        public const decimal FlagMaxRetracement = 0.60m;

        /// <summary>Retracement serre du flag, bonifie dans le score de confiance.</summary>
        public const decimal FlagTightRetracement = 0.40m;

        /// <summary>Hauteur minimale du rectangle rapportee a sa cloture moyenne.</summary>
        public const decimal RectangleMinHeightRatio = 0.025m;

        /// <summary>Hauteur maximale du rectangle rapportee a sa cloture moyenne.</summary>
        public const decimal RectangleMaxHeightRatio = 0.25m;

        /// <summary>Nombre minimal de touches requis sur chaque borne du rectangle.</summary>
        public const int RectangleMinTouchesPerBoundary = 2;

        // --- Garde-fou volatilite ---

        /// <summary>
        /// Plancher applique a l'ATR, exprime en fraction du prix de reference, pour eviter qu'une
        /// volatilite quasi nulle ne rende les seuils ATR degeneres (breakout declenche au moindre
        /// tick).
        /// </summary>
        public const decimal AtrFloorPriceFraction = 0.001m;

        // --- Retournements (fractales / pivots) ---

        /// <summary>
        /// Demi-fenetre N pour la detection de pivots par fractale N-barres.
        /// Un pivot haut en i verifie : close[i] >= close[j] pour tout j dans [i-N, i+N], j != i.
        /// Confirmation exige N bougies apres i (anti look-ahead).
        /// </summary>
        public const int PivotHalfWindow = 3;

        /// <summary>
        /// Tolerance d'egalite entre deux prix pour les figures double top/bottom (Bulkowski).
        /// Un ecart <= 6 % est considere comme egal (deux sommets a hauteur identique).
        /// </summary>
        public const decimal DoublePriceTolerance = 0.06m;

        /// <summary>
        /// Ecart minimal entre les deux sommets/creux d'un double top/bottom, exprime en multiples
        /// d'ATR. Evite de detecter deux pivots consecutifs sur la meme consolidation.
        /// </summary>
        public const decimal DoubleMinSeparationAtrMultiple = 3.0m;

        /// <summary>
        /// Rebond intermédiaire minimal entre les deux creux (double bottom) ou deux sommets
        /// (double top), rapporté à la hauteur de la figure. Bulkowski : >= 10 %.
        /// </summary>
        public const decimal DoubleMinIntermediateReboundPct = 0.10m;

        /// <summary>
        /// Ratio de symetrie des epaules d'une figure tete-epaules (Bulkowski : 0.8 a 1.2).
        /// Rapport largeur epaule gauche / largeur epaule droite doit etre dans [1/ratio, ratio].
        /// </summary>
        public const decimal HsShoulderSymmetryRatio = 1.2m;

        /// <summary>
        /// Profondeur minimale de la tete (H&S) par rapport aux epaules, exprimee en fraction
        /// de la hauteur totale de la figure. La tete doit etre nettement plus profonde/haute.
        /// </summary>
        public const decimal HsMinHeadDepthRatio = 0.10m;

        // --- Support / Resistance overlay ---

        /// <summary>
        /// Nombre minimal de pivots dans un bin ATR pour retenir la zone comme support/resistance.
        /// </summary>
        public const int SrMinTouches = 2;

        /// <summary>
        /// Nombre maximal de zones retournees par le detecteur support/resistance.
        /// Seules les zones avec le plus de touches sont conservees.
        /// </summary>
        public const int SrMaxZones = 8;

        /// <summary>
        /// Fallback de taille de bin quand l'ATR est nul : fraction du prix median.
        /// </summary>
        public const decimal SrAtrFallbackPriceFraction = 0.005m;

        /// <summary>
        /// Nombre de touches a partir duquel la force d'une zone atteint son maximum (1.0).
        /// </summary>
        public const decimal SrStrengthMaxTouches = 5m;
    }
}
