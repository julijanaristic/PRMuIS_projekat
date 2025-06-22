using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KlaseZaIgru.Igrac;

namespace KlaseZaIgru.Rezultati
{
    public class RezultatIgraca
    {
        public Igrac.Igrac Igrac { get; set; }
        public int UkupnoPoena => PoeniSkocko + PoeniSlagalica + PoeniKZZ;
        public int PoeniSkocko {  get; set; }
        public int PoeniKZZ { get; set; }
        public int PoeniSlagalica { get; set; }

        public RezultatIgraca(Igrac.Igrac igrac)
        {
            Igrac = igrac;
        }


    }
}
