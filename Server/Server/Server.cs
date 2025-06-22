using KlaseZaIgru.Igrac;
using KlaseZaIgru.KoZnaZna;
using KlaseZaIgru.Rezultati;
using KlaseZaIgru.Skocko;
using KlaseZaIgru.Slagalica;
//using KlaseZaIgru.Rezultati;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;
using System.Web;


namespace Server
{
    public class Server
    {
        private static Dictionary<Socket, string> stanje = new Dictionary<Socket, string>();
        private static Dictionary<Socket, Queue<string>> igreKlijenta = new Dictionary<Socket, Queue<string>>();
        private static Dictionary<Socket, int> poeni = new Dictionary<Socket, int>();
        private static Dictionary<Socket, Slagalica> slagalice = new Dictionary<Socket, Slagalica>();   
        private static Dictionary<Socket, Skocko> skockoIgre = new Dictionary<Socket, Skocko>();
        private static Dictionary<Socket, KoZnaZna> kzzIgre = new Dictionary<Socket, KoZnaZna>();
        private static Dictionary<Socket, int> pokusajiSkocko = new Dictionary<Socket, int>();
        private static Dictionary<Socket, int> pitanjeKzz = new Dictionary<Socket, int>();
        private static Dictionary<Socket, string> imena = new Dictionary<Socket, string>();
        private static Dictionary<string, string[]> prijaveUDP = new Dictionary<string, string[]>();
        private static Dictionary<int, Dictionary<Socket, int>> kzzOdgovoriPoPitanju = new Dictionary<int, Dictionary<Socket, int>>();
        private static Dictionary<Socket, int> skockoPoeni = new Dictionary<Socket, int>();

        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //za IPv4 se koristi AddressFamily.InterNetwork, SocketType.Dgram je za UDP
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 12345);
            //IPAddress.Any - Koristi se da se utičnica veže na sve dostupne mrežne interfejse
            //12345 - Broj porta na kojem utičnica osluškuje zahteve
            serverSocket.Bind(serverEP);

            Console.WriteLine("Server pokrenut. Čekanje na prijave igrača preko UDP-a.");

            //TcpListener tcpListener = new TcpListener(IPAddress.Parse("192.168.56.1"), 12346);
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("192.168.1.2"), 12346);
            tcpListener.Start();

            List<Socket> klijenti = new List<Socket>();
            string imeIgraca;
            Dictionary<Socket, RezultatIgraca> rezultati = new Dictionary<Socket, RezultatIgraca>();

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

                            //Console.WriteLine($"{imeIgraca} je izbrao/la {brojIgara} igara.\n");

                            //ovde pravim objekat Igrac
                            Igrac igrac = new Igrac(++idIgraca, imeIgraca, brojIgara);

                            //Console.WriteLine(igrac.ToString()); 

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
                }

                //TCP konekcija
                if (tcpListener.Pending())
                {
                    Socket noviSocket = tcpListener.AcceptSocket();
                    noviSocket.Blocking = false;
                    klijenti.Add(noviSocket);
                    stanje[noviSocket] = "ime";
                    Console.WriteLine($"Povezao se novi klijent putem TCP-a: {noviSocket.RemoteEndPoint}");
                }

                //obrada postojecih tcp konekcija
                foreach (Socket klijent in klijenti.ToList())
                {
                    try
                    {
                        if(klijent.Poll(1000, SelectMode.SelectRead))
                        {
                            byte[] buffer = new byte[1024];
                            int bytes = klijent.Receive(buffer);

                            if(bytes == 0)
                            {
                                klijenti.Remove(klijent);
                                Console.WriteLine("Klijent se odvezao.");
                                klijent.Close();
                                continue; //idi na sledeceg klijenta u petlji
                            }

                            string poruka = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                            string stanjeKlijenta = stanje[klijent];

                            if(stanjeKlijenta == "ime")
                            {
                                if (prijaveUDP.ContainsKey(poruka))
                                {
                                    stanje[klijent] = "pocetak";
                                    igreKlijenta[klijent] = new Queue<string>(prijaveUDP[poruka]);
                                    poeni[klijent] = 0;
                                    imena[klijent] = poruka;
                                    klijent.Send(Encoding.UTF8.GetBytes($"Dobrodošli u trening igru kviza TV Slagalica, današnji takmičar je {poruka}. Ako ste spremni, unesite poruku 'SPREMAN': "));
                                } else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Ime nije pronadjeno u UDP prijavama."));
                                    klijent.Close();
                                    klijenti.Remove(klijent);
                                    continue;
                                }
                            } else if(stanjeKlijenta == "pocetak")
                            {
                                if(poruka.ToLower() == "spreman")
                                {
                                    ZapocniSledecuIgru(klijent);

                                    // Provera da li je klijent zatvoren unutar ZapocniSledecuIgru (ako nema više igara)
                                    // Ako je metoda ZapocniSledecuIgru detektovala kraj, onda više neće biti u mapama.
                                    if (!stanje.ContainsKey(klijent))
                                    {
                                        klijent.Close();
                                        klijenti.Remove(klijent);
                                        continue;
                                    }

                                } else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Molimo unesite 'SPREMAN' da biste počeli prvu igru."));
                                }
                            } else if(stanjeKlijenta == "sl")
                            {
                                Slagalica igra = slagalice[klijent];
                                igra.SastavljenaRec = poruka;
                                int p = igra.ProveriRec();
                                poeni[klijent] += p;
                                igreKlijenta[klijent].Dequeue(); //ukloni igru sl iz reda

                                klijent.Send(Encoding.UTF8.GetBytes($"Poeni za reč: {p}. Ukupno: {poeni[klijent]}. Prelaženje na sledeću igru..."));
                                ZapocniSledecuIgru(klijent);

                                if (!stanje.ContainsKey(klijent))
                                {
                                    klijent.Close();
                                    klijenti.Remove(klijent);
                                    continue;
                                }

                            }
                            else if(stanjeKlijenta == "sk")
                            {
                                Skocko igra = skockoIgre[klijent];
                                string rez = igra.ProveriKombinaciju(poruka.ToUpper());
                                pokusajiSkocko[klijent]++;
                                klijent.Send(Encoding.UTF8.GetBytes(rez));

                                if (rez.Contains("Cestitam") || pokusajiSkocko[klijent] >= 6)
                                {
                                    int bodovi = rez.Contains("Cestitam") ? 10 : 0;
                                    poeni[klijent] += bodovi;
                                    igreKlijenta[klijent].Dequeue(); //ukloni igru sk iz reda

                                    klijent.Send(Encoding.UTF8.GetBytes($"Kraj igre Skocko. Poeni za Skocko: {bodovi}. Ukupno: {poeni[klijent]}. Prelaženje na sledeću igru..."));
                                    ZapocniSledecuIgru(klijent);

                                    if (!stanje.ContainsKey(klijent))
                                    {
                                        klijent.Close();
                                        klijenti.Remove(klijent);
                                        continue;
                                    }
                                    
                                }
                                else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes($"Pokusaj {pokusajiSkocko[klijent]++}/6. Unesi 4 znaka (H, T, P, K, S, Z):"));
                                }

                            } else if(stanjeKlijenta == "kzz")
                            {
                                if (int.TryParse(poruka, out int odg) && odg >= 1 && odg <= 3)
                                {
                                    KoZnaZna igra = kzzIgre[klijent];
                                    int poen = 0;
                                    if (!kzzOdgovoriPoPitanju.ContainsKey(pitanjeKzz[klijent]))
                                        kzzOdgovoriPoPitanju[pitanjeKzz[klijent]] = new Dictionary<Socket, int>();
                                    

                                    if (!kzzOdgovoriPoPitanju[pitanjeKzz[klijent]].ContainsKey(klijent))
                                    {
                                        if (igra.ProveriOdgovor(odg) == 10)
                                        {
                                            int brojTacnihOdgovora = kzzOdgovoriPoPitanju[pitanjeKzz[klijent]].Count;
                                            double umanjenje = Math.Pow(0.85, brojTacnihOdgovora);
                                            poen = (int)(10 * umanjenje);
                                        }
                                        else
                                            poen = -5;
                                       
                                        kzzOdgovoriPoPitanju[pitanjeKzz[klijent]][klijent] = poen;
                                    }
                                    
                                    poeni[klijent] += poen;
                                    pitanjeKzz[klijent]++;


                                    if (pitanjeKzz[klijent] < 5)
                                    {
                                        if(poen == -5)
                                        {
                                            klijent.Send(Encoding.UTF8.GetBytes($"Poen: {poen}. Tacan odgovor je: {kzzIgre[klijent].TacanOdgovor}\n\nSledece pitanje:{igra.PostaviSledecePitanje()}"));
                                        }
                                        else
                                        {

                                            klijent.Send(Encoding.UTF8.GetBytes($"Poen: {poen}.\n\nSledece pitanje:{igra.PostaviSledecePitanje()}"));
                                        }

                                    }
                                    else //kraj igre kzz
                                    {
                                        igreKlijenta[klijent].Dequeue(); //ukloni igru kzz iz reda

                                        if(poen == -5)
                                        {
                                            klijent.Send(Encoding.UTF8.GetBytes($"Poen: {poen}. Tacan odgovor je: {kzzIgre[klijent].TacanOdgovor}\n\nKraj igre KO ZNA ZNA. Ukupno: {poeni[klijent]}. Prelaženje na sledeću igru..."));

                                        }else
                                        {
                                            klijent.Send(Encoding.UTF8.GetBytes($"Poen: {poen}.\n\nKraj igre KO ZNA ZNA. Ukupno: {poeni[klijent]}. Prelaženje na sledeću igru..."));

                                        }
                                        ZapocniSledecuIgru(klijent);

                                        if (!stanje.ContainsKey(klijent))
                                        {
                                            klijent.Close();
                                            klijenti.Remove(klijent);
                                            continue;
                                        }
                                    }
                                } else
                                {
                                    klijent.Send(Encoding.UTF8.GetBytes("Neispravan unos. Unesi broj 1-3."));
                                }
                            } 
                        }
                    } catch (SocketException)
                    {
                        stanje.Remove(klijent);
                        igreKlijenta.Remove(klijent);
                        poeni.Remove(klijent);
                        slagalice.Remove(klijent);
                        skockoIgre.Remove(klijent);
                        kzzIgre.Remove(klijent);
                        pokusajiSkocko.Remove(klijent);
                        pitanjeKzz.Remove(klijent);
                        imena.Remove(klijent);
                        klijent.Close();
                        klijenti.Remove(klijent);

                        Console.WriteLine("\nKonačna tabela poena:");
                        foreach(var igrac in imena)
                        {
                            string ime = igrac.Value;
                            int ukupno = poeni.ContainsKey(igrac.Key) ? poeni[igrac.Key] : 0;
                            int skPoen = skockoPoeni.ContainsKey(igrac.Key) ? skockoPoeni[igrac.Key] : 0;
                            Console.WriteLine($"{ime,-15} | Ukupno : {ukupno,3} | Skočko: {skPoen}");
                        }

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
                int poen = poeni[klijent];
                string poruka = $"\n\nSve igre završene. Ukupno poena: {poen}.\n";

                bool pobednik = true;

                foreach(var drugi in poeni)
                {
                    if(drugi.Key != klijent)
                    {
                        if(drugi.Value > poen)
                        {
                            pobednik = false;
                        }
                        else if (drugi.Value == poen)
                        {
                            if(skockoPoeni.ContainsKey(drugi.Key) && skockoPoeni.ContainsKey(klijent))
                            {
                                if (skockoPoeni[drugi.Key] > skockoPoeni[klijent])
                                {
                                    pobednik = false;
                                }
                            }
                        }
                    }
                }

                poruka += pobednik ? "Čestitamo! Vi ste pobednik igre! \n" : "Nažalost, niste pobedili ovaj put.\n";
                poruka += "Hvala na igri!";
                klijent.Send(Encoding.UTF8.GetBytes(poruka));

                //ocistiti sve reference vezane za ovog klijenta
                stanje.Remove(klijent);
                igreKlijenta.Remove(klijent);
                poeni.Remove(klijent);
                slagalice.Remove(klijent);
                skockoIgre.Remove(klijent);
                kzzIgre.Remove(klijent);
                pokusajiSkocko.Remove(klijent);
                pitanjeKzz.Remove(klijent);
                imena.Remove(klijent);

                return;
            }

            string sledecaIgra = igreKlijenta[klijent].Peek().ToString();

            if(sledecaIgra == "sl")
            {
                Slagalica sl = new Slagalica();
                slagalice[klijent] = sl;
                stanje[klijent] = "sl";
                klijent.Send(Encoding.UTF8.GetBytes($"\n------------------Pocinje igra SLAGALICA------------------\n\nSlova: {sl.PonudjenaSlova} \nUnesite reč:"));
            
            } else if(sledecaIgra == "sk")
            {
                Skocko sk = new Skocko();
                skockoIgre[klijent] = sk;
                pokusajiSkocko[klijent] = 0; 
                stanje[klijent] = "sk";
                klijent.Send(Encoding.UTF8.GetBytes($"\n---------------------Pocinje igra SKOCKO---------------------\nUnesi 4 znaka (H, T, P, K, S, Z):"));
            
            } else if(sledecaIgra == "kzz")
            {
                KoZnaZna kzz = new KoZnaZna();
                kzzIgre[klijent] = kzz;
                pitanjeKzz[klijent] = 0; // Resetuj brojač pitanja za KZZ
                stanje[klijent] = "kzz";
                klijent.Send(Encoding.UTF8.GetBytes($"\n---------------------Pocinje igra KO ZNA ZNA---------------------\nImate 5 pitanja ukupno, izaberite tacan odgovor na svako pitanje.\n{kzz.PostaviSledecePitanje()}"));
            
            } else
            {
                klijent.Send(Encoding.UTF8.GetBytes("Server Greška: Nepoznata igra u redu za klijenta. Prekidam vezu."));
            }
        }
    }
}

