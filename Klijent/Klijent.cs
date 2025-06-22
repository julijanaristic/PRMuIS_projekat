using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Klijent
{
    public class Klijent
    {
        static void Main(string[] args)
        {
            Socket klijentUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // PROMENITE IP ADRESU U ZAVISNOSTI OD VAŠE KONFIGURACIJE MREŽE
            IPEndPoint serverUdpEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 12345);
            byte[] buffer = new byte[1024];

            Console.WriteLine("Klijent je spreman za povezivanje sa serverom, pritisnite enter");
            Console.ReadKey();

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

            string listaIgara;
            string[] igre;
            bool validnaListaIgara = false;
            do
            {
                Console.WriteLine("Izaberite igre koje želite igrati [sl, sk, kzz] (odvojene zarezima): ");
                listaIgara = Console.ReadLine()?.Trim();
                igre = listaIgara.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(igra => igra.Trim()).ToArray();

                if (igre.Length > 0 && igre.All(g => g == "sl" || g == "sk" || g == "kzz"))
                {
                    validnaListaIgara = true;
                }
                else
                {
                    Console.WriteLine("Neispravan unos igara. Molimo unesite 'sl', 'sk' i/ili 'kzz' odvojene zarezima.");
                }
            } while (!validnaListaIgara);


            string udpPoruka = $"PRIJAVA: {imeIgraca}, {listaIgara}";
            byte[] udpBajti = Encoding.UTF8.GetBytes(udpPoruka);

            klijentUdpSocket.SendTo(udpBajti, serverUdpEP);
            Console.WriteLine("Uspešno poslata UDP prijava. Čekam potvrdu od servera...");

            EndPoint tempRemoteEP = new IPEndPoint(IPAddress.Any, 0);
            int receivedBytes = klijentUdpSocket.ReceiveFrom(buffer, ref tempRemoteEP);
            string udpResponse = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Server (UDP) odgovor: {udpResponse}");

            if (udpResponse != "UDP_OK")
            {
                Console.WriteLine("UDP prijava nije uspela ili je server vratio grešku. Prekidam rad klijenta.");
                Console.ReadKey();
                klijentUdpSocket.Close();
                return;
            }


            TcpClient tcpClient = null;
            NetworkStream stream = null;
            try
            {
                // PROMENITE IP ADRESU U ZAVISNOSTI OD VAŠE KONFIGURACIJE MREŽE
                tcpClient = new TcpClient("192.168.56.1", 12346);
                stream = tcpClient.GetStream();
                Console.WriteLine("Klijent je uspešno povezan sa serverom putem TCP-a!");

                // Prvo pošalji svoje ime serveru preko TCP-a
                byte[] imeBajti = Encoding.UTF8.GetBytes(imeIgraca);
                stream.Write(imeBajti, 0, imeBajti.Length);
                Console.WriteLine($"Klijent poslao svoje ime preko TCP-a: '{imeIgraca}'");

                // --- Pocetak: Rukovanje prvim "SPREMAN" zahtevom servera ---
                string prviSpreman;
                string initialServerResponse;
                int initialBytesRead;

                bool serverSpremanPorukaPrimljena = false;

                // Prvo pročitaj poruku od servera (koja bi trebalo da sadrži zahtev za "SPREMAN")
                do
                {
                    initialBytesRead = 0; // Resetuj za svaki pokušaj čitanja
                    try
                    {
                        if (stream.DataAvailable) // Proveri da li ima podataka pre čitanja
                        {
                            initialBytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (initialBytesRead > 0)
                            {
                                initialServerResponse = Encoding.UTF8.GetString(buffer, 0, initialBytesRead).Trim();

                                // Sada koristimo TAČAN string koji server šalje (iz vašeg debug loga)
                                if (initialServerResponse.ToLower().Contains("ako ste spremni, unesite poruku 'spreman':"))
                                {
                                    serverSpremanPorukaPrimljena = true;
                                    Console.WriteLine("\nSERVER: " + initialServerResponse); // Ispiši serversku poruku
                                }
                                else
                                {
                                    Console.WriteLine("\nSERVER: " + initialServerResponse); // Ispiši svejedno, ako je neočekivana poruka
                                    Console.WriteLine("Klijent: Prva poruka servera nije sadržala očekivani zahtev za 'SPREMAN'. Pokušavam ponovo...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Klijent: stream.Read() vratio 0 bajtova. Server je možda zatvorio konekciju ili nema više podataka.");
                                break; // Izlazak iz do-while petlje ako je konekcija zatvorena
                            }
                        }
                        else
                        {
                            Thread.Sleep(50); // Kratka pauza ako nema podataka odmah, da se ne vrti prazno
                        }
                    }
                    catch (IOException ex) when ((ex.InnerException as SocketException)?.SocketErrorCode == SocketError.WouldBlock)
                    {
                        // Ovo se dešava ako je socket neblokirajući i nema odmah dostupnih podataka
                        Thread.Sleep(50); // Kratka pauza pre ponovnog pokušaja čitanja
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Klijent: Neplanirana greška prilikom čitanja prve serverske poruke: {ex.Message}");
                        break; // Prekini petlju u slučaju neplanirane greške
                    }

                } while (!serverSpremanPorukaPrimljena);

                // Ako nismo primili 'SPREMAN' poruku, i nismo izašli zbog zatvorene veze (initialBytesRead > 0),
                // to znači da nešto nije u redu i treba prekinuti.
                if (!serverSpremanPorukaPrimljena)
                {
                    Console.WriteLine("Klijent: Nije primljena očekivana prva 'SPREMAN' poruka od servera. Prekidam rad.");
                    return; // Izlazak iz Main metode
                }

                // Sada kada smo sigurni da smo dobili poruku koja traži "SPREMAN", tražimo ga od korisnika
                do
                {
                    Console.Write("Vaš odgovor: ");
                    prviSpreman = Console.ReadLine()?.Trim();
                    if (prviSpreman.ToLower() != "spreman")
                    {
                        Console.WriteLine("Molimo unesite 'SPREMAN' da biste počeli igru.");
                    }
                } while (prviSpreman.ToLower() != "spreman");

                byte[] prviSpremanBajti = Encoding.UTF8.GetBytes(prviSpreman);
                stream.Write(prviSpremanBajti, 0, prviSpremanBajti.Length);
                Console.WriteLine($"Klijent poslao: '{prviSpreman}'");
                // --- Kraj: Rukovanje prvim "SPREMAN" zahtevom servera ---


                // Glavna petlja za igru
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server je zatvorio konekciju. Kraj igre.\n");
                        break;
                    }

                    string serverOdgovor = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine("\nSERVER: " + serverOdgovor);

                    if (serverOdgovor.ToLower().Contains("sve igre završene") || serverOdgovor.ToLower().Contains("hvala na igri") || serverOdgovor.ToLower().Contains("prekidam vezu"))
                    {
                        break;
                    }
                    else if (serverOdgovor.ToLower().Contains("unesite reč:") ||
                             serverOdgovor.ToLower().Contains("unesi 4 znaka") ||
                             serverOdgovor.ToLower().Contains("izaberite tacan odgovor") ||
                             serverOdgovor.ToLower().Contains("unesi broj 1-3:") ||
                             serverOdgovor.ToLower().Contains("pokušaj") || 
                             serverOdgovor.ToLower().Contains("sledece pitanje:"))
                    {
                        // Ako server traži specifičan odgovor (za igru)
                        string odgovorKlijenta;
                        do
                        {
                            Console.Write("Vaš odgovor: ");
                            odgovorKlijenta = Console.ReadLine()?.Trim();

                            if (string.IsNullOrWhiteSpace(odgovorKlijenta))
                            {
                                Console.WriteLine("Odgovor ne može biti prazan! Molimo unesite nešto.");
                            }
                        } while (string.IsNullOrWhiteSpace(odgovorKlijenta));

                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovorKlijenta);
                        stream.Write(odgovorBytes, 0, odgovorBytes.Length);
                        Console.WriteLine($"Klijent poslao: '{odgovorKlijenta}'");
                    }
                    // Ako serverski odgovor ne spada ni u jedan od gore navedenih,
                    // to je verovatno poruka o rezultatu prethodne igre i prelaženju na sledeću.
                    // U tom slučaju, klijent jednostavno nastavlja petlju i čeka sledeću poruku.
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greška u komunikaciji: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                tcpClient?.Close();
                klijentUdpSocket.Close();
                Console.WriteLine("Veza zatvorena. Doviđenja!");
                Console.ReadKey();
            }
        }
    }
}