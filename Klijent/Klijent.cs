using System;
using System.Net;
using System.Net.Sockets;
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
                            Console.WriteLine("Ime nije unijeto! Morate unijeti ime.");
                        }

                    } while (string.IsNullOrEmpty(imeIgraca));

                    Console.WriteLine("Izaberite igre koje želite igrati [sl, sk, kzz] (odvojene zarezima): ");
                    string listaIgara = Console.ReadLine()?.Trim();

                    string poruka = $"PRIJAVA: {imeIgraca}, {listaIgara}";
                    byte[] bajti = Encoding.UTF8.GetBytes(poruka);

                    klijentSocket.SendTo(bajti, klijentEP);
                    Console.WriteLine("Uspješno poslata prijava.");

                    TcpClient tcpClient = new TcpClient("192.168.56.1", 12346);
                    NetworkStream stream = tcpClient.GetStream();

                    byte[] odgovorBajti = new byte[1024];
                    int primljeniBajti = stream.Read(odgovorBajti, 0, odgovorBajti.Length);
                    string odgovor = Encoding.UTF8.GetString(odgovorBajti, 0, primljeniBajti);
                    Console.WriteLine($"Odgovor od servera: {odgovor}");

                    stream.Close();
                    tcpClient.Close();
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška prilikom slanja poruke (klijent): {ex.Message}");
                }
            }

            klijentSocket.Close();
        }
    }
}
