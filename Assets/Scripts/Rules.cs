using UnityEngine;

// Eko Lockdown: core state. Story beats live in Story.cs.
namespace Eko
{
    public static class Game
    {
        public const string NairaSign = "₦";
        public const int Lekki = 5;
        public const int DosePrice = 2500, ZxPrice = 25000;
        public const float InfPerSecond = 0.3f; // 0..100; a street dose knocks off half, so one lasts under three minutes
        public const float DoseAmount = 50f;
        public const float ShowingAt = 55f;     // above this, the veins show and the checkpoint turns you back

        public static int Cash, Doses, Member;
        public static float Inf;
        public static bool Bitten, HasPass, Won;
        public static string Ending, EndingTitle;

        public static string Naira(int n) => NairaSign + n.ToString("N0");
        public static bool IsCovered => Inf < ShowingAt;

        public static void NewGame()
        {
            Cash = 3000; Doses = 0; Inf = 15f; Bitten = true; Member = -1; HasPass = false; Won = false;
            Ending = null; EndingTitle = null;
        }

        public static bool TakeDose()
        {
            if (Doses < 1) return false;
            Doses--; Inf = Mathf.Max(0f, Inf - DoseAmount); return true;
        }

        /// ZX: the fever stops for good.
        public static void Cure() { Bitten = false; Inf = 0f; }

        public static bool BuyDose()
        {
            if (Cash < DosePrice) return false;
            Cash -= DosePrice; Doses++; return true;
        }
    }
}
