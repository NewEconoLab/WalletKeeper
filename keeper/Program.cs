using System;
using System.Threading.Tasks;

namespace keeper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            InitServer();
            //input loop
            while(true)
            {
                var line=Console.ReadLine();
                line = line.Replace(" ", "").ToLower();
                if (line == "exit")
                    break;
            }
        }
        static void InitServer()
        {
            httplib2.RpcServer rpcServer = new httplib2.RpcServer();
            System.Net.IPAddress ipadd = new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 });
            rpcServer.AddParser("/img", ParserImg);
            rpcServer.Start(ipadd, 30080);
        }

        static async Task<object> ParserImg(httplib2.FormData form)
        {
            var f = new httplib2.File();
            f.data = new byte[4];
            return f;
        }
    }
}
