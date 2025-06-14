﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Klijent
{
    public class Klijent
    {
        static void Main(string[] args)
        {
            Socket klijentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint klijentEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 12345);

            while (true)
            {
                try
                {
                    string imeIgraca;
                    do
                    {
                        Console.Write("Unesite vaše ime/nadimak: ");
                        imeIgraca = Console.ReadLine()?.Trim();

                        if (string.IsNullOrEmpty(imeIgraca))
                        {
                            Console.WriteLine("Ime nije uneto! Morate uneti ime.");
                        }

                    } while (string.IsNullOrEmpty(imeIgraca));

                    Console.WriteLine("Izaberite igre koje želite igrati [sl, sk, kzz] (odvojene zarezima): ");
                    string listaIgara = Console.ReadLine()?.Trim();
                    string[] igre = listaIgara.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(igra => igra.Trim()).ToArray(); //ovde imam listu igara sta je igrac odabrao
                    int brojIgara = igre.Length;

                    //Console.WriteLine(brojIgara);

                    string poruka = $"PRIJAVA: {imeIgraca}, {listaIgara}";
                    byte[] bajti = Encoding.UTF8.GetBytes(poruka);

                    klijentSocket.SendTo(bajti, klijentEP);
                    Console.WriteLine("Uspešno poslata prijava.\n");

                    TcpClient tcpClient = new TcpClient("192.168.56.1", 12346);
                    NetworkStream stream = tcpClient.GetStream();

                    byte[] odgovorBajti = new byte[1024];
                    int primljeniBajti = stream.Read(odgovorBajti, 0, odgovorBajti.Length);
                    string odgovor = Encoding.UTF8.GetString(odgovorBajti, 0, primljeniBajti);
                    Console.WriteLine(odgovor);

               
                    while (true)
                    {
                        string rec = "SPREMAN";
                        string spreman;
                        do
                        {
                            Console.WriteLine("Ako ste spremni, unesite poruku 'SPREMAN': ");
                            spreman = Console.ReadLine()?.Trim();

                        } while (spreman != rec && spreman != rec.ToLower());


                        if (spreman == rec || spreman == rec.ToLower())
                        {
                            byte[] spremanBajti = Encoding.UTF8.GetBytes(spreman);
                            stream.Write(spremanBajti, 0, spremanBajti.Length);

                            int i = 0; //ovo sluzi za prolazak kroz listu igara koju je 

                            while (brojIgara > 0)
                            {
                                //Console.WriteLine("igra: '" + igre[i] + "'\n");

                                if (igre[i] == "sl") //SLAGALICA
                                {
                                    //PRIMANJE ISPISA OD SERVERA
                                    byte[] slagalicaBajtovi = new byte[1024];
                                    int bajtoviZaCitanje = stream.Read(slagalicaBajtovi, 0, slagalicaBajtovi.Length);
                                    string primljenaPoruka = Encoding.UTF8.GetString(slagalicaBajtovi, 0, bajtoviZaCitanje);

                                    Console.WriteLine(primljenaPoruka);
                                    Console.WriteLine("Unesite pronadjenu rec: ");
                                    string pronadjenaRec = Console.ReadLine()?.Trim();

                                    //SLANJE PRONADJENE RECI SERVERU
                                    byte[] bajtiPronadjenaRec = Encoding.UTF8.GetBytes(pronadjenaRec);
                                    stream.Write(bajtiPronadjenaRec, 0, bajtiPronadjenaRec.Length);

                                    //PRIMANJE BROJ POENA
                                    byte[] bajtoviZaIspisBrojPoena = new byte[1024];
                                    int bajtoviZaIspisBP = stream.Read(bajtoviZaIspisBrojPoena, 0, bajtoviZaIspisBrojPoena.Length);
                                    string primljenaPorukaZaIspisPoena = Encoding.UTF8.GetString(bajtoviZaIspisBrojPoena, 0, bajtoviZaIspisBP);

                                    Console.WriteLine(primljenaPorukaZaIspisPoena);

                                }
                                else if (igre[i] == "sk") //SKOCKO
                                {
                                    //PRIMANJE ISPISA OD SERVERA
                                    byte[] skockoBajtovi = new byte[1024];
                                    int bajtoviZaIspis = stream.Read(skockoBajtovi, 0, skockoBajtovi.Length);
                                    string primljenaPoruka = Encoding.UTF8.GetString(skockoBajtovi, 0, bajtoviZaIspis);
                                    Console.WriteLine(primljenaPoruka);
                                    
                                    string dozvoljenaSlova = "HTPKSZ";

                                    for(int k = 1; k < 7; k++)
                                    {
                                        string odgovorZaKombinaciju;

                                        while (true)
                                        {
                                            Console.WriteLine("Unesite kombinaciju: ");
                                            odgovorZaKombinaciju = Console.ReadLine().ToUpper().Trim();
                                        
                                            //PROVERAVANJE DA LI JE KORISNIK UNEO ODGOVARAJUCA SLOVA
                                            if (!string.IsNullOrEmpty(odgovorZaKombinaciju) && odgovorZaKombinaciju.ToUpper().All(c => dozvoljenaSlova.Contains(c)))
                                            {
                                                break; //izlazak iz while petlje ako je unos validan
                                            }
                                            else
                                            {
                                                Console.WriteLine("Neispravan unos! Dozvoljena su samo slova: H, T, P, K, S, Z.\nMolimo unesite ponovo.\n");
                                            }
                                            
                                        }

                                        //SALJE SE SERVERU ODGOVOR
                                        byte[] bajtiZaKombinaciju = Encoding.UTF8.GetBytes(odgovorZaKombinaciju);
                                        stream.Write(bajtiZaKombinaciju, 0, bajtiZaKombinaciju.Length);


                                        //PRIMANJE ODGOVORA OD SERVERA
                                        byte[] bajtoviOdServeraZaKombinaciju = new byte[1024];
                                        int bajtoviOdServeraZaKomb = stream.Read(bajtoviOdServeraZaKombinaciju, 0, bajtoviOdServeraZaKombinaciju.Length);
                                        string primljenaPorukaZaKombinaciju = Encoding.UTF8.GetString(bajtoviOdServeraZaKombinaciju, 0, bajtoviOdServeraZaKomb);

                                        Console.WriteLine("\n" + primljenaPorukaZaKombinaciju + "\n");
                                        
                                        if(primljenaPorukaZaKombinaciju == "\nCestitam, pogodili ste kombinaciju!\n")
                                        {
                                            break;
                                        }
                                        
                                    }

                                    //PRIMA SE BROJ POENA OD SERVERA
                                    byte[] bajtiZaPoene = new byte[1024];
                                    int bajtoviZaPoeneOdServera = stream.Read(bajtiZaPoene, 0, bajtiZaPoene.Length);
                                    string porukaZaPoene = Encoding.UTF8.GetString(bajtiZaPoene, 0, bajtoviZaPoeneOdServera);

                                    Console.WriteLine(porukaZaPoene);
                                    

                                }
                                else if (igre[i] == "kzz") //KO ZNA ZNA
                                {
                                    //PRIMANJE ISPISA ZA IGRU
                                    byte[] ispis = new byte[1024];
                                    int bajtoviZaIspis = stream.Read(ispis, 0, ispis.Length);
                                    string ispisZaIgru = Encoding.UTF8.GetString(ispis, 0, bajtoviZaIspis);

                                    
                                    Console.WriteLine(ispisZaIgru);

                                    for(int k = 0; k < 5; k++)
                                    {
                                        //PRIMANJE PITANJA OD SERVERA
                                        byte[] bajtoviZaPitanja = new byte[1024];
                                        int brBajtovaZaPitanja = stream.Read(bajtoviZaPitanja, 0, bajtoviZaPitanja.Length);
                                        string pitanje = Encoding.UTF8.GetString(bajtoviZaPitanja, 0, brBajtovaZaPitanja);

                                        Console.WriteLine(pitanje);

                                        Console.WriteLine("\nUnesite Vas odgovor: ");
                                        string odgovorNaPitanje = Console.ReadLine();

                                        //SALJE SE ODGOVOR SERVERU
                                        byte[] bajtiOdgovora = Encoding.UTF8.GetBytes(odgovorNaPitanje);
                                        stream.Write(bajtiOdgovora, 0, bajtiOdgovora.Length);

                                        //PRIMANJE PORUKE O TACNOSTI ODGOVORA
                                        byte[] bajtoviZaTacnost = new byte[1024];
                                        int brojBajtovaZaTacnost = stream.Read(bajtoviZaTacnost, 0, bajtoviZaTacnost.Length);
                                        string porukaZaTacnost = Encoding.UTF8.GetString(bajtoviZaTacnost, 0, brojBajtovaZaTacnost);

                                        Console.WriteLine(porukaZaTacnost);

                                    }

                                    //PRIMANJE BROJ POENA OD SERVERA
                                    byte[] bajtoviZaPoene = new byte[1024];
                                    int brojBajtovaZaPoene = stream.Read(bajtoviZaPoene, 0, bajtoviZaPoene.Length);
                                    string brojPoena = Encoding.UTF8.GetString(bajtoviZaPoene, 0, brojBajtovaZaPoene);

                                    Console.WriteLine($"{(int.Parse(brojPoena) == 0 ? "Nazalost" : "Cestitam")}, osvojili ste {brojPoena} poena u igri Ko zna zna.\n");

                                }

                                brojIgara--;
                                i++;

                                //Console.WriteLine("broj igara = " + brojIgara);
                                //Console.WriteLine("i = " + i);

                            }

                            break;

                        } else
                        {
                            break;
                        }
                    }
                            stream.Close();
                            tcpClient.Close();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška prilikom slanja poruke (klijent): {ex.Message}");
                    break;
                }
            }

            klijentSocket.Close();
        }
    }
}
