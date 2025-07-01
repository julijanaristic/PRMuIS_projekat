using System;
using System.Collections.Generic;
using System.Linq;

namespace KlaseZaIgru.Slagalica
{
    public class Slagalica
    {
        private Random Random = new Random();
        private static string[] Samoglasnici = { "A", "E", "I", "O", "U" };
        private static string[] Suglasnici = { "B", "C", "D", "DŽ", "F", "G", "H", "J", "K", "L", "LJ", "M", "N", "NJ", "P", "R", "S", "T", "V", "Z" };

        public string PonudjenaSlova { get; private set; }
        public string SastavljenaRec { get; set; }

        public Slagalica()
        {
            GenerisiSlova();
        }

        public Slagalica(string generisanaSlova)
        {
            PonudjenaSlova = generisanaSlova;
        }

        public void GenerisiSlova()
        {
            List<string> slova = new List<string>();

            for (int i = 0; i < 4; i++)
            {
                slova.Add(Samoglasnici[Random.Next(Samoglasnici.Length)]);
            }

            for (int i = 0; i < 8; i++)
            {
                slova.Add(Suglasnici[Random.Next(Suglasnici.Length)]);
            }

            PonudjenaSlova = string.Join(" ", slova);
            Console.WriteLine($"Generisana slova: {PonudjenaSlova}\n");
        }

        public int ProveriRec()
        {
            if (string.IsNullOrEmpty(SastavljenaRec)) return 0;

            SastavljenaRec = SastavljenaRec.ToUpper();

            List<string> dostupnaSlova = PonudjenaSlova.Split(' ').ToList();
            //ključ je svako slovo, a vrednost broj puta koliko se to slovo pojavljuje u listi.
            Dictionary<string, int> dostupnaSlovaDict = dostupnaSlova.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

            List<string> recSlova = new List<string>();
            int i = 0;
            while (i < SastavljenaRec.Length)
            {
                if (i < SastavljenaRec.Length - 1)
                {
                    string dvostrukoSlovo = SastavljenaRec.Substring(i, 2); //uzima se dva uzastopna karaktera, ako i nije poslednji karakter
                    //pokušava da se uzme rec[i] i rec[i+1] kao jedno dvostruko slovo
                    //Ako je ovo dvostruko slovo (dvostrukoSlovo) prisutno u listi dostupnaSlova, ono se dodaje u listu recSlova.
                    //Zatim se i pomera za 2 (jer su dva karaktera već obrađena), a petlja se nastavlja sa sledećim karakterom.
                    if (dostupnaSlova.Contains(dvostrukoSlovo))
                    {
                        recSlova.Add(dvostrukoSlovo);
                        i += 2;
                        continue;
                    }
                }
                recSlova.Add(SastavljenaRec[i].ToString());
                i++;
            }
            //broji koliko puta se svako slovo pojavljuje u unetoj reči.
            Dictionary<string, int> recSlovaDict = recSlova.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());


            foreach (var p in recSlovaDict)
            {
                if (!dostupnaSlovaDict.ContainsKey(p.Key) || dostupnaSlovaDict[p.Key] < p.Value)
                {
                    return 0; //rec nije validna
                }
            }

            return SastavljenaRec.Length * 5; //poeni = 5 * broj slova
        }
    }
}
