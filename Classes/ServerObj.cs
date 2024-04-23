using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerForChat.Classes
{
    public class ServerObj
    {
        static IPAddress Ip = IPAddress.Parse("26.237.33.146");
        static int port = 500;
        TcpListener tcpListener = new TcpListener(Ip, port);
        List<ClientObj> clients = new List<ClientObj>();

        public async Task ListenAsync()
        {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущен");

                while (true)
                {
                    
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    ClientObj client = new ClientObj(tcpClient, this);
                    clients.Add(client);

                    Task.Run(client.ListenClientAsync);

                    Console.WriteLine("Новое подключение!");
                }
            }
            catch(Exception ex)
            { 
                Console.WriteLine(ex.Message.ToString());
            }
        }

        public async Task BroadcastMessageAsync(string message, string login)
        {
            foreach(var client in clients)
            {
                //не отправляем данные отправителю
                if(client.GetLogin != login)
                {
                    //byte[] confirmation = new byte[1] { 0 };
                    //await client.Stream.WriteAsync(confirmation, 0, confirmation.Length);
                    await client.Writer.WriteLineAsync("message");

                    await client.Writer.WriteLineAsync(message);
                    await client.Writer.FlushAsync();
                }
            }
        }

        public async Task BroadcastFile(string pathFile, string login)
        {
            foreach(var client in clients)
            {
                if(client.GetLogin != login)
                {
                    await client.Writer.WriteLineAsync("file");
                    await client.Writer.FlushAsync();

                    await Task.Delay(1000);

                    await client.Writer.WriteLineAsync(login);
                    await client.Writer.FlushAsync();

                    await Task.Delay(1000);


                    byte[] fileData = File.ReadAllBytes(pathFile);

                    byte[] fileSize = BitConverter.GetBytes(fileData.Length);

                    await client.Stream.WriteAsync(fileSize, 0, fileSize.Length);
                    await client.Stream.FlushAsync();

                    await Task.Delay(1000);

                    await client.Stream.WriteAsync(fileData, 0, fileData.Length);
                    await client.Stream.FlushAsync();
                }
            }
        }

        public void RemoveConnectionUser(string login)
        {
            ClientObj clientDisconnection = clients.FirstOrDefault(x => x.GetLogin == login);
            clients.Remove(clientDisconnection);
            clientDisconnection.CloseConnection();
        }

        public void DisconnectAllClients()
        {
            foreach (var client in clients)
            {
                client.CloseConnection();
            }
        }

    }
}
