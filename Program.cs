using ServerForChat.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerForChat
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ServerObj server = new ServerObj();
            await server.ListenAsync();
        }
    }
}
