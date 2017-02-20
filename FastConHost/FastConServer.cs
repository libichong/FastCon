using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
namespace FastConHost
{
    public class FastConServer
    {
        public static string data = null;
        private static bool init = false;
        private static AliasHashTable AliasMapping = null;
        private static Dictionary<string, LinkedList<string>> DirDictionary = new Dictionary<string, LinkedList<string>>();

        public static void StartListening(object ParamPort)
        {
            int Port = Convert.ToInt32(ParamPort);
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                Console.WriteLine(string.Format("dchost is listening on {0}:{1}. Waiting for a connection...", ipAddress.ToString(), Port));
                // Start listening for connections.
                while (true)
                {
                    // Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    data = null;

                    // An incoming connection needs to be processed.
                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOF>") > -1)
                        {
                            data = data.Substring(0, data.Length - 5);
                            break;
                        }
                    }

                    // Show the data on the console.
                    Console.WriteLine("lookup the directory : {0}", data);
                    string msg = "";
                    if (!init)
                    {
                        msg = "Server is indexing the directories!";
                    }
                    else
                    {
                        string[] tokens = data.Split(new char[] { '\\', '/', ' ' });
                        bool found = false;
                        var term = AliasMapping.ContainsKey(tokens[tokens.Length - 1].ToLower()) ? AliasMapping[tokens[tokens.Length - 1].ToLower()] : tokens[tokens.Length - 1].ToLower();
                        if (tokens.Length == 1)
                        {
                            if (DirDictionary.ContainsKey(term))
                            {
                                msg = string.Join("\t", DirDictionary[data.ToLower()].Take(100).ToList());
                                found = true;
                            }
                        }
                        else if (tokens.Length == 2)
                        {
                            if (DirDictionary.ContainsKey(term))
                            {
                                var alias = AliasMapping.ContainsKey(tokens[0].ToLower()) ? AliasMapping[tokens[0].ToLower()] : tokens[0].ToLower();
                                var list = DirDictionary[term].Where(x => x.ToLower().Contains(alias)).ToList();
                                if (list.Count() > 0)
                                {
                                    msg = string.Join("\t", list.Take(100).ToList());
                                    found = true;
                                }
                            }
                        }
                        else if (tokens.Length == 3)
                        {
                            if (DirDictionary.ContainsKey(term.ToLower()))
                            {
                                var alias = AliasMapping.ContainsKey(tokens[0].ToLower()) ? AliasMapping[tokens[0].ToLower()] : tokens[0].ToLower();
                                var alias1 = AliasMapping.ContainsKey(tokens[1].ToLower()) ? AliasMapping[tokens[1].ToLower()] : tokens[1].ToLower();
                                var list = DirDictionary[term].Where(x => x.ToLower().Contains(alias) && x.ToLower().Contains(alias)).ToList();
                                if (list.Count() > 0)
                                {
                                    msg = string.Join("\t", list.Take(100).ToList());
                                    found = true;
                                }
                            }
                        }

                        if (tokens.Length > 3)
                        {
                            msg = string.Format("{0} TOO DEEP!", data);
                        }
                        else if (!found)
                        {
                            msg = string.Format("{0} NOT FOUND!", data);
                        }

                    }
                    // Echo the data back to the client.
                    byte[] msgBuf = Encoding.ASCII.GetBytes(msg);

                    handler.Send(msgBuf);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }
    }
}
