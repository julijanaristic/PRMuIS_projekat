using System;
using System.Diagnostics;

namespace PokretanjeViseKlijenta
{
    public class Pokretanje
    {
        private const int brojKlijenata = 2;

        public void PokreniKlijente()
        {
            for (int i = 0; i < brojKlijenata; i++)
            {
                string klijentPutanja = @"C:\Users\Korisnik\OneDrive\Desktop\PRMuIS_projekat2\Klijent\bin\Debug\Klijent.exe";
                Process klijentProces = new Process();
                klijentProces.StartInfo.FileName = klijentPutanja;
                klijentProces.StartInfo.Arguments = $"{i + 1}"; // argument - broj klijenata
                klijentProces.Start(); //pokretanje klijenta
                Console.WriteLine($"Pokrenut klijent #{i + 1}");
            }
        }
    }
}
