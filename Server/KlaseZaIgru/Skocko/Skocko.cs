using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KlaseZaIgru.Skocko
{
    public class Skocko
    {
        //H - herc, T - tref, P - pik, K - karo, S - skocko, Z - zvezda
        private Random Random = new Random();
        private static char[] Znakovi = { 'H', 'T', 'P', 'K', 'S', 'Z'};

        public string TrazenaKombinacija {  get; set; } //sadrzi 4 slova
        public string TekucaKombinacija { get; set; }

        public Skocko() 
        {
            GenerisiKombinaciju();
        }

        public void GenerisiKombinaciju()
        {
            for(int i = 0; i < 4; i++)
            {
                if(i == 3) //ako je to zadnji karakter 
                {
                    char[] karakteri = TrazenaKombinacija.ToCharArray();
                    char prviKarakter = karakteri[0];
                    int ponavljanje = 0;
                    foreach(var p in karakteri)
                    {
                        if(p == prviKarakter)
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
                } else
                {
                    TrazenaKombinacija += Znakovi[Random.Next(Znakovi.Length)];
                }
            }
            //Console.WriteLine(TrazenaKombinacija);
        }

        public string ProveriKombinaciju(string kombinacija)
        {
            TekucaKombinacija = kombinacija;
            char[] znakoviOdKlijenta = TekucaKombinacija.ToCharArray();
            char[] znakoviOdServera = TrazenaKombinacija.ToCharArray();
            bool[] pogodjeniNaMestu = new bool[4]; //prati koji su tacno pogodjeni
            bool[] iskorisceni = new bool[4]; //prati koji su znakovi servera vec korisceni

            int znakoviNaOdgovarajucemMestu = 0;
            int znakoviNaPogresnomMestu = 0;

            //prvo prolazimo i trazimo znakove na tacnom mestu
            for(int i = 0; i < 4; i++)
            {
                if (znakoviOdKlijenta[i] == znakoviOdServera[i])
                {
                    znakoviNaOdgovarajucemMestu++;
                    pogodjeniNaMestu[i] = true;
                    iskorisceni[i] = true;
                }
            }

            //sada trazimo znakove koji su deo kombinacije ali nisu na pravom mestu
            for(int i = 0; i < 4; i++)
            {
                if (!pogodjeniNaMestu[i]) //znaci gleda ta mesta gde jos nije pogodjeno slovo
                {
                    for(int j = 0; j < 4; j++)
                    {
                        if (!iskorisceni[j] && znakoviOdKlijenta[i] == znakoviOdServera[j])
                        {
                            znakoviNaPogresnomMestu++;
                            iskorisceni[j] = true;
                            break; //sprecava visestruko brojanje istog slova
                        }
                    }
                }
            }

            if(znakoviNaOdgovarajucemMestu == 4)
            {
                return "\nCestitam, pogodili ste kombinaciju!\n";
            }

            string poruka = "";

            if(znakoviNaOdgovarajucemMestu > 0)
            {
                poruka += $"{znakoviNaOdgovarajucemMestu} {(znakoviNaOdgovarajucemMestu == 1 ? "znak je" : "znaka su")} na mestu";
            }

            if(znakoviNaPogresnomMestu > 0)
            {
                if(poruka != "")
                {
                    poruka += " i ";
                }

                poruka += $"{znakoviNaPogresnomMestu} {(znakoviNaPogresnomMestu == 1 ? "znak nije" : "znaka nisu")} na mestu";
            }

            return poruka + ".\n";
        }

    }
}
