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

                    /*byte[] bafer = new byte[1024];
                    EndPoint primljeniEP = new IPEndPoint(IPAddress.Any, 0);
                    int primljeniBajti = klijentSocket.ReceiveFrom(bafer, ref primljeniEP);

                    string odgovor = Encoding.UTF8.GetString(bafer, 0, primljeniBajti);
                    Console.WriteLine($"Odgovor od servera: {odgovor}");*/
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
