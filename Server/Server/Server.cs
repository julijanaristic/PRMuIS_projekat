using KlaseZaIgru.Igrac;
using KlaseZaIgru.KoZnaZna;
using KlaseZaIgru.Skocko;
using KlaseZaIgru.Slagalica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Server
{
    public class Server
    {
        private static List<string> PrijavljeniIgraci = new List<string>();

        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //za IPv4 se koristi AddressFamily.InterNetwork, SocketType.Dgram je za UDP
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 12345);
            serverSocket.Bind(serverEP);
            //IPAddress.Any - Koristi se da se utičnica veže na sve dostupne mrežne interfejse
            //12345 - Broj porta na kojem utičnica osluškuje zahteve

            Console.WriteLine("Server pokrenut. Čekanje na prijave igrača preko UDP-a.");

            while (true)
            {
                byte[] bafer = new byte[1024];
                EndPoint klijentEP = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    int primljeniBajti = serverSocket.ReceiveFrom(bafer, ref klijentEP);
                    string primljenaPoruka = Encoding.UTF8.GetString(bafer, 0, primljeniBajti);

                    Console.WriteLine($"Primljena poruka: {primljenaPoruka}");

                    if(primljenaPoruka.StartsWith("PRIJAVA: "))
                    {
                        string[] dijelovi = primljenaPoruka.Substring(9).Split(new char[] {','}, 2);
                        if(dijelovi.Length == 2)
                        {
                            string imeIgraca = dijelovi[0].Trim();
                            string listaIgara = dijelovi[1].Trim();

                            int idIgraca = 0;
                            string[] igre = listaIgara.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(igra => igra.Trim()).ToArray(); //ovde imam listu igara sta je igrac odabrao
                            int brojIgara = igre.Length;

                            //Console.WriteLine($"{imeIgraca} je izbrao/la {brojIgara} igara.\n");

                            //ovde pravim objekat Igrac
                            Igrac igrac = new Igrac(++idIgraca, imeIgraca, brojIgara);

                            //Console.WriteLine(igrac.ToString()); 

                            if (ValidacijaIgara(listaIgara))
                            {
                                Console.WriteLine($"Prijava uspešna\n\nIme igrača: {imeIgraca}\nLista igara: {listaIgara}\n");

                                PrijavljeniIgraci.Add(imeIgraca);

                                TcpListener tcpListener = new TcpListener(IPAddress.Parse("192.168.56.1"), 12346);
                                tcpListener.Start();

                                Console.WriteLine($"Čekanje na TCP vezu za igrača {imeIgraca}");
                                Socket tcpSocket = tcpListener.AcceptSocket();
                                //TcpClient klijent = tcpListener.AcceptTcpClient(); //prihvati vezu kao TcpClient
                                NetworkStream stream = new NetworkStream(tcpSocket);
                                Console.WriteLine($"Usmerena TCP veza za {imeIgraca}");

                                string dobrodoslicaPoruka = $"Dobrodošli u trening igru kviza TV Slagalica, današnji takmičar je {imeIgraca}.";
                                if (PrijavljeniIgraci.Count() > 1)
                                    dobrodoslicaPoruka = $"Dobrodošli u igru kviza TV Slagalica, današnji takmičari su {string.Join(", ", PrijavljeniIgraci)}.";

                                byte[] dobrodoslicaBajti = Encoding.UTF8.GetBytes(dobrodoslicaPoruka);
                                tcpSocket.Send(dobrodoslicaBajti);
                                Console.WriteLine($"Poslata poruka dobrodošlice: {dobrodoslicaPoruka}");

                                byte[] spremanBajti = new byte[1024];
                                int primljeniSpremanBajti = tcpSocket.Receive(spremanBajti);
                                string spremanPoruka = Encoding.UTF8.GetString(spremanBajti, 0, primljeniSpremanBajti);

                                string spreman = "SPREMAN";
                                if (spremanPoruka == spreman.ToLower() || spremanPoruka == spreman)
                                {
                                    Console.WriteLine($"Igrač {imeIgraca} je spreman za početak igre.");


                                    int i = 0; //ovo sluzi za prolazak kroz listu igara koju je 

                                    while (brojIgara > 0)
                                    {
                                        if (igre[i] == "sl") //SLAGALICA
                                        {
                                            string poruka = "\n------------------Pocinje igra SLAGALICA------------------\n\n";
                                            Slagalica igra = new Slagalica();
                                            poruka += igra.PonudjenaSlova + "\n";

                                            //SALJE SE ISPIS KLIJENTU
                                            byte[] bajtovi = Encoding.UTF8.GetBytes(poruka);
                                            stream.Write(bajtovi, 0, bajtovi.Length);
                                            Console.WriteLine($"\nPoslata slova klijentu: {igra.PonudjenaSlova}\n");

                                            //PRONADJENA REC OD KLIJENTA
                                            byte[] bajtoviPrimljenaRec = new byte[1024];
                                            int bajtoviZaCitanjePrimljenaRec = stream.Read(bajtoviPrimljenaRec, 0, bajtoviPrimljenaRec.Length);
                                            string primljenaRec = Encoding.UTF8.GetString(bajtoviPrimljenaRec, 0, bajtoviZaCitanjePrimljenaRec);

                                            Console.WriteLine($"\nPrimljena rec od klijenta: {primljenaRec}. Proveravanje u toku...\n");

                                            //PROVERA PRONADJENE RECI
                                            igra.SastavljenaRec = primljenaRec;
                                            int brojPoena = igra.ProveriRec();
                                            Console.WriteLine($"Broj poena igraca za pronadjenu reč: {brojPoena}");

                                            //prvo u poenima uvek bude slagalica, pa skocko, pa ko zna zna
                                            igrac.BrojPoenaPoIgrama[i] = brojPoena; //ovde sad ne znam kako treba dodeliti
                                            Console.WriteLine(igrac.ToString());

                                            //SALJE SE BROJ POENA KLIJENTU
                                            //string brojPoenaString = brojPoena.ToString();
                                            string porukaZaIspisPoenaKlijentu = $"\nOsvojili ste {brojPoena} poena za unetu rec.\n";
                                            byte[] bajtoviZaIspisPoena = Encoding.UTF8.GetBytes(porukaZaIspisPoenaKlijentu);
                                            stream.Write(bajtoviZaIspisPoena, 0, bajtoviZaIspisPoena.Length);

                                            Console.WriteLine("\nKRAJ IGRE SLAGALICA\n");
                                        }
                                        else if (igre[i] == "sk") //SKOCKO
                                        {

                                            string poruka = "\n---------------------Pocinje igra SKOCKO---------------------\n";
                                            poruka += "H - herc, T - tref, P - pik, K - karo, S - skocko, Z - zvezda\n";
                                            Skocko skocko = new Skocko();
                                            string TrazenaKomb = skocko.TrazenaKombinacija;

                                            Console.WriteLine("\nPocinje igra SKOCKO\n");
                                            Console.WriteLine($"Trazena kombinacija: {TrazenaKomb}\n");

                                            //SALJE SE ISPIS KLIJENTU
                                            byte[] bajtoviZaTrazenuKombinaciju = Encoding.UTF8.GetBytes(poruka);
                                            stream.Write(bajtoviZaTrazenuKombinaciju, 0, bajtoviZaTrazenuKombinaciju.Length);

                                            int brojPoena = 30;
                                            int greska = 0;

                                            string porukaZaIspisPogodjenihZnakova = "";

                                            for (int k = 1; k < 7; k++)
                                            {
                                                //PRIMANJE ODGOVORA ZA TEKUCU KOMBINACIJU
                                                byte[] bajtoviZaTekucuKombinaciju = new byte[1024];
                                                int bajtoviZaTekKomb = stream.Read(bajtoviZaTekucuKombinaciju, 0, bajtoviZaTekucuKombinaciju.Length);
                                                string TekKombinacija = Encoding.UTF8.GetString(bajtoviZaTekucuKombinaciju, 0, bajtoviZaTekKomb).Trim();
                                                //Trim() da uklonimo nevidljive karaktere

                                                string TekKombinacijaCAPS = TekKombinacija.ToUpper();

                                                Console.WriteLine($"\nPrimljena kombinacija od klijenta: '{TekKombinacijaCAPS}', Pokusaj: {k}. Proveravanje u toku...\n");
                                                
                                                //PROVERA VALIDNOSTI UNOSA
                                                string dozvoljenaSlova = "HTPKSZ";

                                                if(!string.IsNullOrEmpty(TekKombinacijaCAPS) && TekKombinacijaCAPS.All(c => dozvoljenaSlova.Contains(c)))
                                                {
                                                    //PROVERAVANJE KOMBINACIJE
                                                    Console.WriteLine($"Klijent je poslao validan odgovor: {TekKombinacijaCAPS}");
                                                    porukaZaIspisPogodjenihZnakova = skocko.ProveriKombinaciju(TekKombinacijaCAPS);
                                                
                                                    //SALJE SE KLIJENTU POVRATNA PORUKA SA PROVERE
                                                    byte[] bajtoviSaProvere = Encoding.UTF8.GetBytes(porukaZaIspisPogodjenihZnakova);
                                                    stream.Write(bajtoviSaProvere, 0, bajtoviSaProvere.Length);
                                                
                                                    if (porukaZaIspisPogodjenihZnakova == "\nCestitam, pogodili ste kombinaciju!\n")
                                                    {
                                                        if (k == 5 || k == 6)
                                                        {
                                                            brojPoena = 10;
                                                        }
                                                        else
                                                        {
                                                            brojPoena -= greska;
                                                        }
                                                        Console.WriteLine("Igrac je pogogio kombinaciju!\nKRAJ IGRE SKOCKO\n");
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        greska += 5;
                                                    }

                                                } else
                                                {
                                                    Console.WriteLine("Neispravan unos od klijenta. Odbijeno.\n");
                                                    porukaZaIspisPogodjenihZnakova = "Neispravan unos. Dozvoljena su samo slova: H, T, P, K, S, Z\n";
                                                    byte[] bajtoviSaProvere = Encoding.UTF8.GetBytes(porukaZaIspisPogodjenihZnakova);
                                                    stream.Write(bajtoviSaProvere, 0, bajtoviSaProvere.Length);
                                                    k--; //ne brojimo nevalidan pokusaj
                                                }
                                            }
                                            
                                            //znaci kad se izvrtela for petlja, 6 pokusaja, i ako na kraju ne stoji u poruci da je pogodio igrac kombinaciju,
                                            //to znaci da nije uspeo pogoditi tokom 6 pokusaja, dobija 0 poena;
                                            if(porukaZaIspisPogodjenihZnakova != "\nCestitam, pogodili ste kombinaciju!\n")
                                            {
                                                brojPoena = 0;
                                            }

                                            //SALJE SE BROJ POENA KLIJENTU
                                            string brojPoenaString = $"\nOsvojili ste {brojPoena} poena za unetu rec.\n";
                                            //Console.WriteLine(brojPoenaString);
                                            byte[] bajtiZaPoeneKlijentu = Encoding.UTF8.GetBytes(brojPoenaString);
                                            stream.Write(bajtiZaPoeneKlijentu, 0, bajtiZaPoeneKlijentu.Length);

                                            igrac.BrojPoenaPoIgrama[i] = brojPoena;
                                            Console.WriteLine(igrac.ToString());

                                        }
                                        else if (igre[i] == "kzz") //KO ZNA ZNA
                                        {
                                            Console.WriteLine("\nPocinje igra KO ZNA ZNA\n");

                                            string poruka = "\n---------------------Pocinje igra KO ZNA ZNA---------------------\n";
                                            KoZnaZna koZnaZna = new KoZnaZna();
                                            int ukupniPoeni = 0;

                                            //SLANJE ISPISA KLIJENTU
                                            byte[] ispis = Encoding.UTF8.GetBytes(poruka);
                                            stream.Write(ispis, 0, ispis.Length);


                                            for(int k = 0; k < 5; k++)
                                            {
                                                //SLANJE PITANJA KLIJENTU
                                                string pitanej = koZnaZna.PostaviSledecePitanje();
                                                byte[] pitanjeBajti = Encoding.UTF8.GetBytes(pitanej);
                                                stream.Write(pitanjeBajti, 0, pitanjeBajti.Length);

                                                //PRIMANJE ODGOVORA OD KLIJENTA
                                                byte[] bajtiOdgovora = new byte[1024];
                                                int bajtiOdgovoraKlijenta = stream.Read(bajtiOdgovora, 0, bajtiOdgovora.Length);
                                                string odgovor = Encoding.UTF8.GetString(bajtiOdgovora, 0, bajtiOdgovoraKlijenta);
                                                int brojOdgovora = int.Parse(odgovor);

                                                //PROVERA ODGOVORA 
                                                int osvojeniPoen = koZnaZna.ProveriOdgovor(brojOdgovora);
                                             
                                                //SALJE SE KLIJENTU PORUKA O TACNOSTI ODGOVORA NA PITANJE
                                                string porukaZaUspesanOdgovor = $"Vas odgovor je {(osvojeniPoen == 10 ? "tacan" : "netacan")}.\n";
                                                if (porukaZaUspesanOdgovor.Contains("netacan"))
                                                {
                                                    porukaZaUspesanOdgovor += $"Tacan odgovor je: {koZnaZna.TacanOdgovor}\n";
                                                }
                                                byte[] bajtoviZaTacanOdg = Encoding.UTF8.GetBytes(porukaZaUspesanOdgovor);
                                                stream.Write(bajtoviZaTacanOdg, 0, bajtoviZaTacanOdg.Length);
                                               
                                                ukupniPoeni += osvojeniPoen;
                                            }

                                            ukupniPoeni = (ukupniPoeni < 0 ? 0 : ukupniPoeni);
                                            igrac.BrojPoenaPoIgrama[i] = ukupniPoeni;
                                            Console.WriteLine(igrac.Ime + $" je osvojio {ukupniPoeni} poena.");
                                            Console.WriteLine(igrac.ToString());

                                            //SLANJE BROJ POENA IGRACU
                                            string brojPoena = ukupniPoeni.ToString();
                                            byte[] poeni = Encoding.UTF8.GetBytes(brojPoena);
                                            stream.Write(poeni, 0, poeni.Length);

                                        }

                                        brojIgara--;
                                        i++;

                                    }
                                    
                                } 
                                
                                tcpSocket.Close();
                                tcpListener.Stop();

                            }
                            else
                            {
                                Console.WriteLine("Nevalidna lista igara.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Nepravilno formatirana prijava.");
                        }


                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška prilikom prijema podataka na serveru: {ex.Message}");
                    break;
                }

            }

            serverSocket.Close(); //zatvara se uticnica, oslobadjaju se resursi, za ovo da bi se izvrsilo, treba dodati break u while petlju, inace nece nikad da se izvrsi
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
    }
}
