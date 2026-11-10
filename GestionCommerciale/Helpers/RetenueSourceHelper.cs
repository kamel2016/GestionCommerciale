namespace GestionCommerciale.Helpers
{
    public static class RetenueSourceHelper
    {
        public const decimal SeuilApplication = 1000m;

        /// <summary>
        /// Calcule la RS sur le TTC hors timbre.
        /// </summary>
        public static decimal Calculer(decimal totalTtc, decimal tauxPourcent)
        {
            if (tauxPourcent <= 0 || totalTtc < SeuilApplication)
                return 0m;

            return Math.Round(totalTtc * tauxPourcent / 100m, 3, MidpointRounding.AwayFromZero);
        }
    }
}