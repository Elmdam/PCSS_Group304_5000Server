using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 1234;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        private static int[] clientScores = new int[3];
        private static int currentSocket;
        private static int n;

        private static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine();
            CloseAllSockets();
        }

        private static void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");

        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            serverSocket.Close();
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            int received;
            Socket current = (Socket) AR.AsyncState;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client disconnected");

                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Text:" + text);

            switch (text.ToLower())
            {
                default:
                    Console.WriteLine("Chat message received");
                    byte[] data = Encoding.ASCII.GetBytes(text);

                    Console.WriteLine("Message sent");
                    break;


                case "exit":

                    current.Shutdown(SocketShutdown.Both);
                    current.Close();
                    clientSockets.Remove(current);
                    Console.WriteLine("Client disconnected");
                    return;

                case "end turn":
                    Console.WriteLine("client ends turn... sending turn to next client");
                    data = Encoding.ASCII.GetBytes("your turn");
                    if (clientSockets.Count == 3)
                    {
                        if (currentSocket == 0)
                        {
                            currentSocket = 1;
                            clientSockets[1].Send(data);

                        }
                        else if (currentSocket == 1)
                        {
                            clientSockets[2].Send(data);
                            currentSocket++;
                            Console.WriteLine("hello2");
                        }
                        else if (currentSocket == 2)
                        {
                            clientSockets[0].Send(data);
                            currentSocket = 0;
                        }
                    }

                    Console.WriteLine("Message sent");
                    break;

                case "message":
                    string mesTex = Console.ReadLine();
                    byte[] mesData = Encoding.ASCII.GetBytes(mesTex);
                    foreach (Socket sockets in clientSockets)
                    {

                        sockets.Send(mesData);
                    }
                    break;

                case "start game":
                    if (clientSockets.Count == 3)
                    {
                        data = Encoding.ASCII.GetBytes("your turn");
                        clientSockets[0].Send(data);

                        data = Encoding.ASCII.GetBytes("game started");
                        clientSockets[1].Send(data);

                        data = Encoding.ASCII.GetBytes("game started");
                        clientSockets[2].Send(data);
                    }
                    else
                    {
                        data = Encoding.ASCII.GetBytes("Not enough players yet");
                        foreach (Socket sockets in clientSockets)
                        {

                            sockets.Send(data);
                        }
                    }
                    break;

                case "updatescores":

                    break;



            }
            CheckIfScore(text, current);




            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
        static void CheckIfScore(string _text, Socket current)
        {
            n = 0;
            bool isNumeric = int.TryParse(_text, out n);

            if (current == clientSockets[0])
            {
                clientScores[0] += n;
            }
            else if (current == clientSockets[1])
            {
                clientScores[1] += n;
            }
            else if (current == clientSockets[2])
            {
                clientScores[2] += n;
            }

            for (int i = 0; i < clientScores.Length; i++)
            {
                Console.WriteLine(clientScores[i]);
            }
        }
    }
}