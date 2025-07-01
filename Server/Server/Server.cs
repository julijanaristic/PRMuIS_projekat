using KlaseZaIgru.Igrac;
using KlaseZaIgru.KoZnaZna;
using KlaseZaIgru.Skocko;
using KlaseZaIgru.Slagalica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Server
{
    public class Server
    {
        private static string imeIgraca;
        private static Dictionary<Socket, string> imena = new Dictionary<Socket, string>();
        private static Dictionary<Socket, Queue<string>> igreKlijenta = new Dictionary<Socket, Queue<string>>();
        private static Dictionary<Socket, string> stanje = new Dictionary<Socket, string>();
        private static Dictionary<Socket, int> poeni = new Dictionary<Socket, int>();
        private static Dictionary<string, string[]> prijaveUDP = new Dictionary<string, string[]>();
        private static Dictionary<Socket, bool> igraciSpremni = new Dictionary<Socket, bool>();
        private static List<Socket> klijenti = new List<Socket>();
        private static Dictionary<string, string> modPoIgracu = new Dictionary<string, string>();

        //za slagalicu
        private static Slagalica zajednickaSlagalica;
        private static string zajednickaGenerisanaSlova;
        private static Dictionary<Socket, bool> odgovorPoReci = new Dictionary<Socket, bool>();

        //za skocko
        private static Skocko zajednickiSkocko;
        private static string zajednickaGenerisanaKombinacija;
        private static Dictionary<Socket, bool> odgovorenoUSkockoPokusaju = new Dictionary<Socket, bool>();
        private static Dictionary<Socket, int> pokusajiSkocko = new Dictionary<Socket, int>();
        private static Dictionary<Socket, string> zadnjaKombinacija = new Dictionary<Socket, string>();
        private static Socket prviPogodioKombinaciju = null;
        private static Dictionary<Socket, int> skockoPoeni = new Dictionary<Socket, int>();

        //za ko zna zna
        private static KoZnaZna zajednickiKzz;
        private static List<string> zajednickaPitanja;
        private static int trenutnoPitanjeKzz = 0;
        private static Dictionary<Socket, int> pitanjeKzz = new Dictionary<Socket, int>();
        private static Dictionary<Socket, bool> odgovorenoNaTrenutnoPitanje = new Dictionary<Socket, bool>();
        private static Dictionary<int, List<Socket>> tacniOdgovoriPoPitanju = new Dictionary<int, List<Socket>>();
        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //za IPv4 se koristi AddressFamily.InterNetwork, SocketType.Dgram je za UDP
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 12345);
            //IPAddress.Any - Koristi se da se utičnica veže na sve dostupne mrežne interfejse
            //12345 - Broj porta na kojem utičnica osluškuje zahteve
            serverSocket.Bind(serverEP);

            Console.WriteLine("Server pokrenut. Čekanje na prijave igrača preko UDP-a.");

            TcpListener tcpListener = new TcpListener(IPAddress.Any, 12346);
            //TcpListener tcpListener = new TcpListener(IPAddress.Parse("192.168.56.1"), 12346);
            //TcpListener tcpListener = new TcpListener(IPAddress.Parse("192.168.1.2"), 12346);
            tcpListener.Start();


            while (true)
            {
                if (serverSocket.Poll(0, SelectMode.SelectRead))
                {
                    byte[] bafer = new byte[1024];
                    EndPoint klijentEP = new IPEndPoint(IPAddress.Any, 0);
                    int primljeniBajti = serverSocket.ReceiveFrom(bafer, ref klijentEP);
                    string primljenaPoruka = Encoding.UTF8.GetString(bafer, 0, primljeniBajti);

                    Console.WriteLine($"Primljena poruka: {primljenaPoruka}");

                    if (primljenaPoruka.StartsWith("PRIJAVA: "))
                    {
                        string[] dijelovi = primljenaPoruka.Substring(9).Split(new char[] { ',' }, 2);
                        if (dijelovi.Length == 2)
                        {
                            imeIgraca = dijelovi[0].Trim();
                            string listaIgara = dijelovi[1].Trim();

                            int idIgraca = 0;
                            string[] igre = listaIgara.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(igra => igra.Trim()).ToArray(); //ovde imam listu igara sta je igrac odabrao
                            int brojIgara = igre.Length;

                            Igrac igrac = new Igrac(++idIgraca, imeIgraca, brojIgara);

                            if (ValidacijaIgara(listaIgara))
                            {
                                prijaveUDP[imeIgraca] = igre;
                                Console.WriteLine($"Prijava uspešna\n\nIme igrača: {imeIgraca}\nLista igara: {listaIgara}\n");

                                Console.WriteLine($"Čekanje na TCP vezu za igrača {imeIgraca}");
                                serverSocket.SendTo(Encoding.UTF8.GetBytes("UDP_OK"), klijentEP);

                            }
                            else
                            {
                                Console.WriteLine("Nevalidna lista igara.");
                                serverSocket.SendTo(Encoding.UTF8.GetBytes("UDP_INVALID_GAMES"), klijentEP);

                            }
                        }
                    }

                    if (primljenaPoruka.StartsWith("MOD: "))
                    {
                        string mod = primljenaPoruka.Substring(5).Trim().ToUpper();
                        Console.WriteLine($"Klijent izabrao mod: {mod}");

                        modPoIgracu[imeIgraca] = mod;

                        continue; // vrati se na cekanje sledece poruke
                    }
                }

                //TCP konekcija, prihvatanje klijenta
                if (tcpListener.Pending())
                {
                    Socket noviSocket = tcpListener.AcceptSocket();
                    noviSocket.Blocking = false;
                    klijenti.Add(noviSocket);
                    poeni[noviSocket] = 0;
                    stanje[noviSocket] = "ime";
                    igraciSpremni[noviSocket] = false; //svi na pocetku nisu spremni
                    Console.WriteLine($"Povezao se novi klijent putem TCP-a: {noviSocket.RemoteEndPoint}");

                }

                //obrada postojecih tcp konekcija
                foreach (Socket klijent in klijenti.ToList())
                {
                    try
                    {
                        if (klijent.Poll(1000, SelectMode.SelectRead))
                        {
                            byte[] buffer = new byte[1024];
                            int bytes = klijent.Receive(buffer);

                            if (bytes == 0)
                            {
                                klijenti.Remove(klijent);
                                Console.WriteLine("Klijent se odvezao.");
                                klijent.Close();
                                continue; //idi na sledeceg klijenta u petlji
                            }

                            string poruka = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                            string stanjeKlijenta = stanje[klijent];

                            if (stanjeKlijenta == "ime")
                            {
                                if (prijaveUDP.ContainsKey(poruka) && !imena.ContainsValue(poruka))
                                {
                                    imena[klijent] = poruka;
                                    igreKlijenta[klijent] = new Queue<string>(prijaveUDP[poruka]);
                                    stanje[klijent] = "pocetak";

                                    if (modPoIgracu[imena[klijent]].ToLower() == "trening")
                                    {
                                        klijent.Send(Encoding.UTF8.GetBytes($"Dobrodošli u trening igru kviza TV Slagalica, današnji takmičar je {poruka}. Ako ste spremni, unesite poruku 'SPREMAN': "));
                                    }
                                    else if (modPoIgracu[imena[klijent]].ToLower() == "takmicenje")
                                    {
                                        klijent.Send(Encoding.UTF8.GetBytes($"Dobrodošli u igru kviza TV Slagalica, današnji takmičar je {poruka}. Ako ste spremni, unesite poruku 'SPREMAN': "));
                                    }


                                    Console.WriteLine($"TCP identifikacija: {poruka} ({klijent.RemoteEndPoint})");
                                }
                                else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Ime nije validno ili je već zauzeto. Prekid veze."));
                                    klijenti.Remove(klijent);
                                    Console.WriteLine("Klijent se odvezao.");
                                    klijent.Close();
                                    continue; //idi na sledeceg klijenta u petlji
                                }
                            }
                            else if (stanjeKlijenta == "pocetak")
                            {
                                if (poruka.ToLower() == "spreman")
                                {
                                    igraciSpremni[klijent] = true;
                                    klijent.Send(Encoding.UTF8.GetBytes("Vasa spremnost je zabelezena. Cekamo ostale igrace..."));

                                    if (igraciSpremni.Values.All(v => v)) //kad su svi rekli spreman
                                    {
                                        Console.WriteLine("Svi igraci su spremni! Pocinje igra za sve!");

                                        foreach (var igrac in klijenti)
                                        {
                                            igrac.Send(Encoding.UTF8.GetBytes("Svi su rekli SPREMAN! Krecemo odmah!\n"));
                                            ZapocniSledecuIgru(igrac);

                                            // Provera da li je klijent zatvoren unutar ZapocniSledecuIgru (ako nema više igara)
                                            // Ako je metoda ZapocniSledecuIgru detektovala kraj, onda više neće biti u mapama.
                                            if (!stanje.ContainsKey(igrac))
                                            {
                                                igrac.Close();
                                                klijenti.Remove(igrac);
                                                continue;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Molimo unesite 'SPREMAN' da biste počeli prvu igru."));
                                }
                            }
                            else if (stanjeKlijenta == "sl")
                            {
                                Slagalica igra = new Slagalica(zajednickaGenerisanaSlova);
                                igra.SastavljenaRec = poruka;
                                int p = igra.ProveriRec();
                                poeni[klijent] += p;
                                odgovorPoReci[klijent] = true;

                                klijent.Send(Encoding.UTF8.GetBytes($"Poeni za rec: {p}. Ukupno: {poeni[klijent]}. Prelazenje na sledecu igru..."));

                                if (odgovorPoReci.Values.All(v => v))
                                {
                                    foreach (var k in klijenti)
                                    {
                                        igreKlijenta[k].Dequeue();
                                        k.Send(Encoding.UTF8.GetBytes("Svi su odgovorili! Prelazimo na sledecu igru...\n"));
                                        ZapocniSledecuIgru(k);
                                    }
                                }
                            }
                            else if (stanjeKlijenta == "sk")
                            {
                                zadnjaKombinacija[klijent] = poruka.ToUpper();

                                if (zadnjaKombinacija[klijent].Length != 4)
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Kombinacija mora imati tačno 4 znaka (H, T, P, K, S, Z). Pokušaj ponovo:"));
                                    continue;
                                }

                                Skocko igra = new Skocko(zajednickaGenerisanaKombinacija);
                                pokusajiSkocko[klijent]++;
                                string rez = igra.ProveriKombinaciju(zadnjaKombinacija[klijent]);
                                odgovorenoUSkockoPokusaju[klijent] = true;
                                Console.WriteLine($"Trazena kombinacija je: {igra.TrazenaKombinacija}\n");

                                if (!rez.Contains("Cestitam"))
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes(rez));
                                }
                                else
                                {
                                    // Detektujemo prvog koji je pogodio
                                    if (prviPogodioKombinaciju == null)
                                    {
                                        prviPogodioKombinaciju = klijent;
                                    }
                                }

                                // Kada svi odgovore u toj rundi
                                if (odgovorenoUSkockoPokusaju.Values.All(v => v))
                                {
                                    bool sviIskoristiliPokusaje = klijenti.All(k => pokusajiSkocko[k] >= 6);

                                    if (prviPogodioKombinaciju != null)
                                    {
                                        // Neko je pogodio – proglasavamo kraj
                                        foreach (var k in klijenti)
                                        {
                                            if (k == prviPogodioKombinaciju)
                                            {
                                                int pokusaj = pokusajiSkocko[k];
                                                int bodovi = 0;
                                                switch (pokusaj)
                                                {
                                                    case 1: bodovi = 30; break;
                                                    case 2: bodovi = 25; break;
                                                    case 3: bodovi = 20; break;
                                                    case 4: bodovi = 15; break;
                                                    case 5: bodovi = 10; break;
                                                    case 6: bodovi = 10; break;
                                                    default: bodovi = 0; break;
                                                }

                                                if (!skockoPoeni.ContainsKey(klijent))
                                                    skockoPoeni[klijent] = 0;

                                                skockoPoeni[k] = bodovi;
                                                poeni[k] += bodovi;
                                                k.Send(Encoding.UTF8.GetBytes($"Cestitam! Pogodili ste kombinaciju u pokušaju {pokusaj}. Poeni za Skočko: {bodovi}. Ukupno: {poeni[k]}.\n"));
                                            }
                                            else
                                            {
                                                skockoPoeni[k] = 0;
                                                k.Send(Encoding.UTF8.GetBytes($"Igrač {imena[prviPogodioKombinaciju]} je pogodio/la kombinaciju pre vas. Vi niste osvojili poene za Skočko. Ukupno: {poeni[k]}.\n"));
                                            }
                                            igreKlijenta[k].Dequeue();
                                            ZapocniSledecuIgru(k);
                                        }
                                        odgovorenoUSkockoPokusaju.Clear();
                                        prviPogodioKombinaciju = null; // resetuj za sledeći put
                                        continue;
                                    }
                                    else if (sviIskoristiliPokusaje)
                                    {
                                        // Niko nije pogodio, svi iskoristili pokušaje
                                        foreach (var k in klijenti)
                                        {
                                            skockoPoeni[k] = 0;
                                            k.Send(Encoding.UTF8.GetBytes($"Niste pogodili kombinaciju. Poeni za Skočko: 0. Ukupno: {poeni[k]}.\n"));
                                            igreKlijenta[k].Dequeue();
                                            ZapocniSledecuIgru(k);
                                        }
                                        odgovorenoUSkockoPokusaju.Clear();
                                        continue;
                                    }
                                    else
                                    {
                                        // Niko nije pogodio, ima još pokušaja – resetuj za sledeći krug
                                        foreach (var k in klijenti)
                                        {
                                            odgovorenoUSkockoPokusaju[k] = false;
                                            k.Send(Encoding.UTF8.GetBytes($"Pokusaj {pokusajiSkocko[k] + 1}/6. Unesi 4 znaka (H, T, P, K, S, Z):"));
                                        }
                                    }
                                }
                            }
                            else if (stanjeKlijenta == "kzz")
                            {
                                if (int.TryParse(poruka, out int odg) && odg >= 1 && odg <= 3)
                                {
                                    string trenutnoPitanje = zajednickaPitanja[trenutnoPitanjeKzz];
                                    int tacanOdgovor = zajednickiKzz.SvaPitanja[trenutnoPitanje];

                                    if (!tacniOdgovoriPoPitanju.ContainsKey(trenutnoPitanjeKzz))
                                        tacniOdgovoriPoPitanju[trenutnoPitanjeKzz] = new List<Socket>();

                                    int poen;

                                    if (odg == tacanOdgovor)
                                    {
                                        int brojPrethodnihTacnih = tacniOdgovoriPoPitanju[trenutnoPitanjeKzz].Count;
                                        double umanjenje = Math.Pow(0.85, brojPrethodnihTacnih);
                                        poen = (int)(10 * umanjenje);

                                        tacniOdgovoriPoPitanju[trenutnoPitanjeKzz].Add(klijent);
                                    }
                                    else
                                    {
                                        poen = -5;
                                    }

                                    poeni[klijent] += poen;
                                    odgovorenoNaTrenutnoPitanje[klijent] = true;

                                    klijent.Send(Encoding.UTF8.GetBytes($"Poen: {poen}. Tacan odgovor je: {tacanOdgovor}\n"));

                                    //proveri da li su svi odgovorili na trenutno pitanje
                                    if (odgovorenoNaTrenutnoPitanje.Values.All(v => v))
                                    {
                                        trenutnoPitanjeKzz++;
                                        if (trenutnoPitanjeKzz < 5)
                                        {
                                            tacniOdgovoriPoPitanju[trenutnoPitanjeKzz] = new List<Socket>();
                                            //resetuj status i salji sledece pitanje
                                            foreach (var k in klijenti)
                                            {
                                                odgovorenoNaTrenutnoPitanje[k] = false;
                                                k.Send(Encoding.UTF8.GetBytes($"Sledece pitanje:\n{zajednickaPitanja[trenutnoPitanjeKzz]}"));
                                            }
                                        }
                                        else
                                        {
                                            //kraj kzz
                                            foreach (var k in klijenti)
                                            {
                                                igreKlijenta[k].Dequeue();
                                                k.Send(Encoding.UTF8.GetBytes($"Kraj igre KO ZNA ZNA. Ukupno poena: {poeni[k]}.\n"));
                                                ZapocniSledecuIgru(k);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Neispravan unos. Unesi broj 1-3:"));
                                }
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        stanje.Remove(klijent);
                        igreKlijenta.Remove(klijent);
                        poeni.Remove(klijent);
                        pokusajiSkocko.Remove(klijent);
                        pitanjeKzz.Remove(klijent);
                        imena.Remove(klijent);
                        klijent.Close();
                        klijenti.Remove(klijent);

                        break;
                    }
                }
            }
            Console.WriteLine("Server zavrsava sa radom");
            Console.ReadKey();
            serverSocket.Close();
        }
        private static bool ValidacijaIgara(string listaIgara)
        {
            string[] validneIgre = { "sl", "sk", "kzz" };
            string[] igre = listaIgara.Split(',');
            foreach (var igra in igre)
            {
                if (!validneIgre.Contains(igra.Trim()))
                    return false;
            }
            return true;
        }

        private static void ZapocniSledecuIgru(Socket klijent)
        {
            //provera da li ima jos igraca za ovog klijenta
            if (igreKlijenta[klijent].Count == 0)
            {
                igreKlijenta.Remove(klijent);
                klijent.Send(Encoding.UTF8.GetBytes($"Sve igre zavrsene. Ukupno poena: {poeni[klijent]}.\n"));

                if (igreKlijenta.Count == 0) // svi su završili
                {
                    ProglasiPobednika();
                }
                return;
            }

            string sledecaIgra = igreKlijenta[klijent].Peek().ToString();

            if (sledecaIgra == "sl")
            {
                if (zajednickaSlagalica == null)
                {
                    zajednickaSlagalica = new Slagalica();
                }
                //slagalice[klijent] = zajednickaSlagalica;
                zajednickaGenerisanaSlova = zajednickaSlagalica.PonudjenaSlova;
                stanje[klijent] = "sl";
                odgovorPoReci[klijent] = false;
                klijent.Send(Encoding.UTF8.GetBytes($"\n------------------Pocinje igra SLAGALICA------------------\n\nSlova: {zajednickaSlagalica.PonudjenaSlova} \nUnesite rec:"));

            }
            else if (sledecaIgra == "sk")
            {
                if (zajednickiSkocko == null)
                {
                    zajednickiSkocko = new Skocko();
                }
                //skockoIgre[klijent] = zajednickiSkocko;
                zajednickaGenerisanaKombinacija = zajednickiSkocko.TrazenaKombinacija;
                pokusajiSkocko[klijent] = 0;
                stanje[klijent] = "sk";
                odgovorenoUSkockoPokusaju[klijent] = false;
                klijent.Send(Encoding.UTF8.GetBytes($"\n---------------------Pocinje igra SKOCKO---------------------\nUnesi 4 znaka (H, T, P, K, S, Z):"));

            }
            else if (sledecaIgra == "kzz")
            {
                if (zajednickiKzz == null)
                {
                    zajednickiKzz = new KoZnaZna();
                    zajednickaPitanja = zajednickiKzz.PitanjaLista.Take(5).ToList();

                }

                pitanjeKzz[klijent] = 0; // Resetuj brojač pitanja za KZZ
                stanje[klijent] = "kzz";
                odgovorenoNaTrenutnoPitanje[klijent] = false;
                klijent.Send(Encoding.UTF8.GetBytes($"\n---------------------Pocinje igra KO ZNA ZNA---------------------\nImate 5 pitanja ukupno, izaberite tacan odgovor na svako pitanje.\n{zajednickaPitanja[0]}"));

            }
            else
            {
                klijent.Send(Encoding.UTF8.GetBytes("Server Greska: Nepoznata igra u redu za klijenta. Prekidam vezu."));
            }
        }

        private static void ProglasiPobednika()
        {
            Console.WriteLine("\nKonačna tabela poena:");
            foreach (var igrac in imena)
            {
                string ime = igrac.Value;
                int ukupno = poeni.ContainsKey(igrac.Key) ? poeni[igrac.Key] : 0;
                int skPoen = skockoPoeni.ContainsKey(igrac.Key) ? skockoPoeni[igrac.Key] : 0;
                Console.WriteLine($"{ime,-15} | Ukupno: {ukupno,3} | Skocko: {skPoen}");
            }

            // Traženje pobednika
            var pobednik = imena.Keys.First();
            foreach (var klijent in imena.Keys)
            {
                if (poeni[klijent] > poeni[pobednik])
                {
                    pobednik = klijent;
                }
                else if (poeni[klijent] == poeni[pobednik])
                {
                    // Ako su izjednačeni, proveri Skocko poene
                    int sk1 = skockoPoeni.ContainsKey(klijent) ? skockoPoeni[klijent] : 0;
                    int sk2 = skockoPoeni.ContainsKey(pobednik) ? skockoPoeni[pobednik] : 0;
                    if (sk1 > sk2) pobednik = klijent;
                }
            }

            // Pošalji rezultat svima
            foreach (var klijent in imena.Keys)
            {
                if (klijent == pobednik)
                {
                    klijent.Send(Encoding.UTF8.GetBytes("\n\n===========================================================\n\nCestitamo! Vi ste pobednik igre!\n"));
                }
                else
                {
                    klijent.Send(Encoding.UTF8.GetBytes("\n\n===========================================================\n\nNazalost, niste pobedili ovaj put.\n"));
                }
            }
        }
    }
}

