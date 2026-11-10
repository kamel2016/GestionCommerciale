namespace GestionCommerciale.Helpers
{
    public static class MontantEnLettres
    {
        private static readonly string[] Unites =
        {
            "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
            "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize",
            "dix-sept", "dix-huit", "dix-neuf"
        };

        private static readonly string[] Dizaines =
        {
            "", "", "vingt", "trente", "quarante", "cinquante", "soixante"
        };

        /// <summary>
        /// Convertit un montant en dinars tunisiens (3 décimales) en lettres.
        /// Ex : 120.500 => "cent vingt dinars et cinq cents millimes"
        /// </summary>
        public static string ConvertirDinars(decimal montant)
        {
            montant = Math.Round(montant, 3, MidpointRounding.AwayFromZero);
            var negatif = montant < 0;
            montant = Math.Abs(montant);

            var dinars = (long)Math.Truncate(montant);
            var millimes = (int)Math.Round((montant - dinars) * 1000, MidpointRounding.AwayFromZero);

            string resultat;

            if (dinars == 0 && millimes == 0)
            {
                resultat = "zéro dinar";
            }
            else
            {
                var parties = new List<string>();

                if (dinars > 0)
                    parties.Add($"{Nombre(dinars)} {(dinars > 1 ? "dinars" : "dinar")}");

                if (millimes > 0)
                    parties.Add($"{Nombre(millimes)} {(millimes > 1 ? "millimes" : "millime")}");

                resultat = string.Join(" et ", parties);
            }

            if (negatif) resultat = "moins " + resultat;

            // Majuscule initiale
            return char.ToUpper(resultat[0]) + resultat[1..];
        }

        // ---------- Conversion d'un entier en lettres ----------

        private static string Nombre(long n)
        {
            if (n == 0) return "zéro";

            var parts = new List<string>();

            // Milliards
            var milliards = n / 1_000_000_000;
            n %= 1_000_000_000;
            if (milliards > 0)
                parts.Add($"{Nombre(milliards)} milliard{(milliards > 1 ? "s" : "")}");

            // Millions
            var millions = n / 1_000_000;
            n %= 1_000_000;
            if (millions > 0)
                parts.Add($"{Nombre(millions)} million{(millions > 1 ? "s" : "")}");

            // Milliers ("mille" est invariable, et on ne dit pas "un mille")
            var milliers = n / 1000;
            n %= 1000;
            if (milliers > 0)
                parts.Add(milliers == 1 ? "mille" : $"{Centaines(milliers, false)} mille");

            // Reste (< 1000) : "quatre-vingts" / "cents" prennent le s seulement en fin de nombre
            if (n > 0)
                parts.Add(Centaines(n, true));

            return string.Join(" ", parts);
        }

        /// <param name="finDeNombre">true si rien ne suit : active les accords (cents, quatre-vingts)</param>
        private static string Centaines(long n, bool finDeNombre)
        {
            var c = n / 100;
            var reste = n % 100;
            var texte = "";

            if (c > 0)
            {
                if (c == 1)
                    texte = "cent";
                else
                    texte = $"{Unites[c]} cent" + (reste == 0 && finDeNombre ? "s" : "");
            }

            if (reste > 0)
            {
                if (texte.Length > 0) texte += " ";
                texte += Dizaine(reste, finDeNombre);
            }

            return texte;
        }

        private static string Dizaine(long n, bool finDeNombre)
        {
            if (n < 20) return Unites[n];

            var d = n / 10;
            var u = n % 10;

            // 70-79 : soixante-dix...
            if (d == 7)
            {
                var reste = 10 + u;
                return u == 1 ? "soixante et onze" : $"soixante-{Unites[reste]}";
            }

            // 80-99 : quatre-vingt...
            if (d == 8)
            {
                if (u == 0) return finDeNombre ? "quatre-vingts" : "quatre-vingt";
                return $"quatre-vingt-{Unites[u]}";
            }
            if (d == 9)
            {
                return $"quatre-vingt-{Unites[10 + u]}";
            }

            // 20-69
            var dizaine = Dizaines[d];
            if (u == 0) return dizaine;
            if (u == 1) return $"{dizaine} et un";
            return $"{dizaine}-{Unites[u]}";
        }
    }
}