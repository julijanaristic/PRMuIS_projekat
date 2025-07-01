using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KlaseZaIgru.KoZnaZna
{
    public class KoZnaZna
    {
        public string TekucePitanje { get; private set; }
        public int TacanOdgovor { get; private set; } //broj opcije koja predstavlja tacan odgovor

        public Dictionary<string, int> SvaPitanja { get; set; } = new Dictionary<string, int>();

        public List<string> PitanjaLista { get; set; } = new List<string>();
        //private int TrenutnoPitanje = 0;
        public KoZnaZna()
        {
            UcitavajPitanja("pitanja.txt");
        }

        public KoZnaZna(List<string> vecGenerisanaPitanja, Dictionary<string, int> svaPitanja)
        {
            PitanjaLista = vecGenerisanaPitanja;
            SvaPitanja = svaPitanja;
        }

        //PITANJA SU U FORMATU
        //Pitanje;Opcija1;Opcija2;Opcija3;BrojTacnoOdgovora
        public void UcitavajPitanja(string putanjaDoFajla)
        {
            if (!File.Exists(putanjaDoFajla))
            {
                Console.WriteLine($"Fajl sa pitanjima nije pronađen na putanji: {Path.GetFullPath(putanjaDoFajla)}");
                return;
            }
            string[] linije = File.ReadAllLines(putanjaDoFajla);
            foreach (string linija in linije)
            {
                string[] delovi = linija.Split(';');

                if (delovi.Length == 5 && int.TryParse(delovi[4].Trim(), out int tacan))
                {
                    string pitanje = $"{delovi[0]}\n{delovi[1]}\n{delovi[2]}\n{delovi[3]}";
                    SvaPitanja.Add(pitanje, tacan);
                    PitanjaLista.Add(pitanje);
                }
            }

            Random random = new Random();
            PitanjaLista = PitanjaLista.OrderBy(x => random.Next()).ToList();
        }

        public int ProveriOdgovor(int odgovor)
        {
            return odgovor == TacanOdgovor ? 10 : -5;
        }


        public string PostaviSledecePitanje()
        {

            if (PitanjaLista.Count == 0)
            {
                return "Kviz je zavrsen!";
            }

            Random random = new Random();
            int indeks = random.Next(PitanjaLista.Count); //nasumicno bira pitanje

            TekucePitanje = PitanjaLista[indeks];
            TacanOdgovor = SvaPitanja[TekucePitanje];

            //uklanjamo postavljeno pitanje da se ne ponavlja
            PitanjaLista.RemoveAt(indeks);

            return TekucePitanje;
        }
    }
}
