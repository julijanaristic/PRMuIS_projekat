using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 12345);
            serverSocket.Bind(serverEP);

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

                            if (ValidacijaIgara(listaIgara))
                            {
                                Console.WriteLine($"Prijava uspješna\nIme igrača: {imeIgraca}\nLista igara: {listaIgara}");

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
                }
            }

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
    }
}
