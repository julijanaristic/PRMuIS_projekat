using System;

namespace KlaseZaIgru.Skocko
{
    public class Skocko
    {
        //H - herc, T - tref, P - pik, K - karo, S - skocko, Z - zvezda
        private Random Random = new Random();
        private static char[] Znakovi = { 'H', 'T', 'P', 'K', 'S', 'Z' };

        public string TrazenaKombinacija { get; set; }
        public string TekucaKombinacija { get; set; }

        public Skocko()
        {
            GenerisiKombinaciju();
        }

        public Skocko(string generisanaKombinacija)
        {
            TrazenaKombinacija = generisanaKombinacija;
        }

        public void GenerisiKombinaciju()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == 3) 
                {
                    char[] karakteri = TrazenaKombinacija.ToCharArray();
                    char prviKarakter = karakteri[0];
                    int ponavljanje = 0;
                    foreach (var p in karakteri)
                    {
                        if (p == prviKarakter)
                        {
                            ++ponavljanje;
                        }
                    }

                    if (ponavljanje == 3)
                    {
                        while (true)
                        {
                            char zadnjiKarakter = Znakovi[Random.Next(Znakovi.Length)];
                            if (zadnjiKarakter != prviKarakter)
                            {
                                TrazenaKombinacija += zadnjiKarakter;
                                break;
                            }

                        }
                    }
                    else
                    {
                        TrazenaKombinacija += Znakovi[Random.Next(Znakovi.Length)];
                    }
                }
                else
                {
                    TrazenaKombinacija += Znakovi[Random.Next(Znakovi.Length)];
                }
            }
        }

        public string ProveriKombinaciju(string kombinacija)
        {
            TekucaKombinacija = kombinacija;
            char[] znakoviOdKlijenta = TekucaKombinacija.ToCharArray();
            char[] znakoviOdServera = TrazenaKombinacija.ToCharArray();
            bool[] pogodjeniNaMestu = new bool[4]; 
            bool[] iskorisceni = new bool[4]; 

            int znakoviNaOdgovarajucemMestu = 0;
            int znakoviNaPogresnomMestu = 0;

            for (int i = 0; i < 4; i++)
            {
                if (znakoviOdKlijenta[i] == znakoviOdServera[i])
                {
                    znakoviNaOdgovarajucemMestu++;
                    pogodjeniNaMestu[i] = true;
                    iskorisceni[i] = true;
                }
            }

            for (int i = 0; i < 4; i++) 
            {
                if (!pogodjeniNaMestu[i]) 
                {
                    for (int j = 0; j < 4; j++) 
                    {
                        if (!iskorisceni[j] && znakoviOdKlijenta[i] == znakoviOdServera[j])
                        {
                            znakoviNaPogresnomMestu++;
                            iskorisceni[j] = true;
                            break; 
                        }
                    }
                }
            }

            if (znakoviNaOdgovarajucemMestu == 4)
            {
                return "\nCestitam, pogodili ste kombinaciju!\n";
            }

            string poruka = "";

            if (znakoviNaOdgovarajucemMestu > 0)
            {
                poruka += $"{znakoviNaOdgovarajucemMestu} {(znakoviNaOdgovarajucemMestu == 1 ? "znak je" : "znaka su")} na mestu";
            }

            if (znakoviNaPogresnomMestu > 0)
            {
                if (!string.IsNullOrEmpty(poruka))
                {
                    poruka += " i ";
                }

                poruka += $"{znakoviNaPogresnomMestu} {(znakoviNaPogresnomMestu == 1 ? "znak nije" : "znaka nisu")} na mestu";
            }

            if (string.IsNullOrEmpty(poruka))
            {
                return "Nema pogodataka.\n";
            }

            return poruka + ".\n";
        }

    }
}
