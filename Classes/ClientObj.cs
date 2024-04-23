using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data.Entity;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerForChat.Classes
{
    public class ClientObj
    {
        private string Login;
        public StreamWriter Writer { get; }
        protected StreamReader Reader { get; }
        public NetworkStream Stream { get; }
        protected ChattDBEntities contextBD;
        private static int countNewFile = 0;

        TcpClient Client;
        ServerObj Server;

        public string GetLogin
        {
            get
            {
                return Login;
            }
        }

        public ClientObj(TcpClient tcpClient, ServerObj server)
        {
            Client = tcpClient;
            Server = server;
            Stream = Client.GetStream();
            Reader = new StreamReader(Stream);
            Writer = new StreamWriter(Stream);
            contextBD = new ChattDBEntities();
        }

        public async Task ListenClientAsync()
        {
            string message;

            while (true)
            {
                byte[] AuthOrReg = new byte[1];

                await Stream.ReadAsync(AuthOrReg, 0, AuthOrReg.Length);

               
                if (AuthOrReg[0] == 1)
                {
                    string login;
                    string password;
                    byte[] answerAuth = new byte[1];

                    login = await Reader.ReadLineAsync();

                    password = await Reader.ReadLineAsync();

                    var clientConnect = await contextBD.Users.FirstOrDefaultAsync(x => x.Login == login);

                    if(clientConnect == null)
                    {
                        answerAuth[0] = 0;
                        await Stream.WriteAsync(answerAuth, 0, answerAuth.Length);
                    }
                    else
                    {
                        if(clientConnect.Password == password)
                        {
                            answerAuth[0] = 1;
                            await Stream.WriteAsync(answerAuth, 0, answerAuth.Length);

                            Login = clientConnect.Login;
                            Console.WriteLine($"{Login} вошел в чат!");
                            await Server.BroadcastMessageAsync($"{Login} вошел в чат!", Login);

                            try
                            {



                                while (true)
                                {

                                    string typeData = await Reader.ReadLineAsync();
                                    try
                                    {
                                        //Получение сообщения от пользователя
                                        switch (typeData)
                                        {
                                            case "message":
                                                message = await Reader.ReadLineAsync();
                                                if (message == null)
                                                    continue;
                                                message = $"{Login}: {message}";
                                                Console.WriteLine(message);

                                                await Server.BroadcastMessageAsync(message, Login);
                                                break;
                                            case "file":
                                                await ReadFileAsync();
                                                break;
                                        }

                                    }
                                    catch
                                    {
                                        
                                    }


                                }
                            }
                            catch
                            {
                                message = $"{Login} покинул чат";
                                Console.WriteLine(message);
                                await Server.BroadcastMessageAsync(message, Login);
                            }
                            finally
                            {
                                Server.RemoveConnectionUser(Login);
                            }


                        }
                        else
                        {
                            answerAuth[0] = 0;
                            await Stream.WriteAsync(answerAuth, 0, answerAuth.Length);
                        }
                    }

                }
                else if (AuthOrReg[0] == 0)
                {
                    string login = await Reader.ReadLineAsync();
                    string password = await Reader.ReadLineAsync();
                    string firstName = await Reader.ReadLineAsync();
                    string lastName = await Reader.ReadLineAsync();

                    var clientConnect =  await contextBD.Users.FirstOrDefaultAsync(x => x.Login == login);
                    byte[] answerUser = new byte[] { 0 };

                    if (clientConnect == null)
                    {
                        Users user = new Users();

                        user.FirstName = firstName;
                        user.LastName = lastName;
                        user.Password = password;
                        user.Login = login;

                        contextBD.Users.Add(user);

                        try
                        {
                            contextBD.SaveChanges();
                            answerUser[0] = 1;
                            await Stream.WriteAsync(answerUser, 0, answerUser.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                            answerUser[0] = 2;
                            await Stream.WriteAsync(answerUser, 0, answerUser.Length);
                        }

                    }
                    else
                    {
                        await Stream.WriteAsync(answerUser, 0, answerUser.Length);

                    }
                }

                


                
            }
        }

        private async Task ReadFileAsync()
        {
            string nameFolderForSave = "ImageUsers";

            byte[] fileSizeBytes = new byte[sizeof (int)];

            //получение размера файла
            await Stream.ReadAsync(fileSizeBytes, 0, fileSizeBytes.Length);
            int size = BitConverter.ToInt32(fileSizeBytes, 0);
            
            byte[] fileData = new byte[size];

            //получение данных файла
            await Stream.ReadAsync(fileData, 0, fileData.Length);

            string formatFile = ".jpeg";

            if (fileData[4] == 0x66 && fileData[5] == 0x74 && fileData[6] == 0x79
                            && fileData[7] == 0x70)
            {
                formatFile = ".MP4";
                nameFolderForSave = "VideoUsers";
            }

            
            string PATH_FILE = $"{Environment.CurrentDirectory}/{nameFolderForSave}/{Login}_{countNewFile}{formatFile}";

            try
            {
                
                //сохранение файла
                File.WriteAllBytes(PATH_FILE, fileData);
                
                switch (formatFile)
                {
                    case ".jpeg":
                        Console.WriteLine("Получено новое изображение: " + Login + "_" + countNewFile + formatFile);
                        break;
                    case ".MP4":
                        Console.WriteLine("Получено новое видео: " + Login + countNewFile + formatFile);
                        break;

                }
                countNewFile++;
                await Server.BroadcastFile(PATH_FILE, Login);
            }
            catch( Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            
        }

        public void CloseConnection()
        {
            Reader.Close();
            Writer.Close();
            Stream.Close();
            Client.Close();
        }
    }
}
