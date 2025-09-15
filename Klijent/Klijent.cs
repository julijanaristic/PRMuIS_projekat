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
            //IPEndPoint serverUdpEP = new IPEndPoint(IPAddress.Parse("192.168.56.1"), 12345); // Kaca
            IPEndPoint serverUdpEP = new IPEndPoint(IPAddress.Parse("192.168.1.2"), 12345); // Jana
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

            string listaIgara = "";
            string[] igre;
            bool validnaListaIgara = false;

            string odgovor = "";
            do
            {
                Console.WriteLine("Izaberite da li hocete 'TAKMICENJE' ili 'TRENING': ");
                odgovor = Console.ReadLine()?.Trim().ToLower();

                if (odgovor.Equals("takmicenje"))
                {
                    listaIgara = "sl, sk, kzz";
                    break;
                }
                else if (odgovor.Equals("trening"))
                {
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
                    break;

                }
                else
                {
                    Console.WriteLine("\nNiste ispravno uneli vasu zelju.");
                }
            } while (odgovor != "takmicenje" || odgovor != "trening");

            string udpPoruka = $"PRIJAVA: {imeIgraca}, {listaIgara}";
            byte[] udpBajti = Encoding.UTF8.GetBytes(udpPoruka);

            klijentUdpSocket.SendTo(udpBajti, serverUdpEP);
            Console.WriteLine("Uspešno poslata UDP prijava. Čekam potvrdu od servera...");

            string udpModPoruka = $"MOD: {odgovor.ToUpper()}";
            byte[] udpModBajti = Encoding.UTF8.GetBytes(udpModPoruka);
            klijentUdpSocket.SendTo(udpModBajti, serverUdpEP);

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
                //tcpClient = new TcpClient("192.168.56.1", 12346); // Kaca
                tcpClient = new TcpClient("192.168.1.2", 12346); // Jana
                stream = tcpClient.GetStream();
                Console.WriteLine("Klijent je uspešno povezan sa serverom putem TCP-a!");

                byte[] imeBajti = Encoding.UTF8.GetBytes(imeIgraca);
                stream.Write(imeBajti, 0, imeBajti.Length);
                Console.WriteLine($"Klijent poslao svoje ime preko TCP-a: '{imeIgraca}'");

                string prviSpreman;
                string initialServerResponse;
                int initialBytesRead;

                bool serverSpremanPorukaPrimljena = false;

                do
                {
                    initialBytesRead = 0; 
                    try
                    {
                        if (stream.DataAvailable) 
                        {
                            initialBytesRead = stream.Read(buffer, 0, buffer.Length);
                            if (initialBytesRead > 0)
                            {
                                initialServerResponse = Encoding.UTF8.GetString(buffer, 0, initialBytesRead).Trim();

                                if (initialServerResponse.ToLower().Contains("ako ste spremni, unesite poruku 'spreman':"))
                                {
                                    serverSpremanPorukaPrimljena = true;
                                    Console.WriteLine("\nSERVER: " + initialServerResponse); 
                                }
                                else
                                {
                                    Console.WriteLine("\nSERVER: " + initialServerResponse); 
                                    Console.WriteLine("Klijent: Prva poruka servera nije sadržala očekivani zahtev za 'SPREMAN'. Pokušavam ponovo...");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Klijent: stream.Read() vratio 0 bajtova. Server je možda zatvorio konekciju ili nema više podataka.");
                                break; 
                            }
                        }
                        else
                        {
                            Thread.Sleep(50); 
                        }
                    }
                    catch (IOException ex) when ((ex.InnerException as SocketException)?.SocketErrorCode == SocketError.WouldBlock)
                    {
                        Thread.Sleep(50); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Klijent: Neplanirana greška prilikom čitanja prve serverske poruke: {ex.Message}");
                        break; 
                    }

                } while (!serverSpremanPorukaPrimljena);

                if (!serverSpremanPorukaPrimljena)
                {
                    Console.WriteLine("Klijent: Nije primljena očekivana prva 'SPREMAN' poruka od servera. Prekidam rad.");
                    return; 
                }

                do
                {
                    Console.Write("Vas odgovor: ");
                    prviSpreman = Console.ReadLine()?.Trim();
                    if (prviSpreman.ToLower() != "spreman")
                    {
                        Console.WriteLine("Molimo unesite 'SPREMAN' da biste počeli igru.");
                    }
                } while (prviSpreman.ToLower() != "spreman");

                byte[] prviSpremanBajti = Encoding.UTF8.GetBytes(prviSpreman);
                stream.Write(prviSpremanBajti, 0, prviSpremanBajti.Length);
                Console.WriteLine($"Klijent poslao: '{prviSpreman}'");

                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server je zatvorio konekciju. Kraj igre.\n");
                        break;
                    }

                    string serverOdgovor = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"\n{serverOdgovor}");

                    if (serverOdgovor.ToLower().Contains("prekidam vezu") ||
                        serverOdgovor.ToLower().Contains("hvala na igri") ||
                        serverOdgovor.ToLower().Contains("dovidenja"))
                    {
                        break;
                    }
                    else if (serverOdgovor.ToLower().Contains("unesite rec:") ||
                             serverOdgovor.ToLower().Contains("unesi 4 znaka") ||
                             serverOdgovor.ToLower().Contains("izaberite tacan odgovor") ||
                             serverOdgovor.ToLower().Contains("unesi broj 1-3:") ||
                             serverOdgovor.ToLower().Contains("pokusajte ponovo:") ||
                             serverOdgovor.ToLower().Contains("sledece pitanje:"))
                    {
                        string odgovorKlijenta;
                        do
                        {
                            Console.Write("Vas odgovor: ");
                            odgovorKlijenta = Console.ReadLine()?.Trim();

                            if (string.IsNullOrWhiteSpace(odgovorKlijenta))
                            {
                                Console.WriteLine("Odgovor ne moze biti prazan! Molimo unesite nesto.");
                            }
                        } while (string.IsNullOrWhiteSpace(odgovorKlijenta));

                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovorKlijenta);
                        stream.Write(odgovorBytes, 0, odgovorBytes.Length);
                    }

                    if (serverOdgovor.ToLower().Contains("cestitamo") ||
                        serverOdgovor.ToLower().Contains("nazalost"))
                    {
                        Console.WriteLine("\n======================== KRAJ IGRE ========================");
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greska u komunikaciji: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                tcpClient?.Close();
                klijentUdpSocket.Close();
                Console.WriteLine("Veza zatvorena. Dovidjenja!");
                Console.ReadKey();
            }
        }
    }
}