namespace KlaseZaIgru.Igrac
{
    public class Igrac
    {
        public int BrojIgraca { get; set; }
        public string Ime { get; set; }
        public int[] BrojPoenaPoIgrama { get; set; }

        public Igrac(int brojIgraca, string ime, int brojIgara)
        {
            BrojIgraca = brojIgraca;
            Ime = ime;
            BrojPoenaPoIgrama = new int[brojIgara];
        }

        public override string ToString()
        {
            string igrac = $"\nBroj igraca: {BrojIgraca}\nIme: {Ime}\nBroj poena: ";

            foreach (var p in BrojPoenaPoIgrama)
            {
                igrac += p + " ";
            }

            return igrac;
        }
    }
}
