using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace httplib2
{
    public delegate Task<object> onHttp(FormData data);
    public class File : Microsoft.Extensions.FileProviders.IFileInfo
    {

        public byte[] data;

        public bool Exists => true;
        public long Length => data.Length;

        static readonly System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
        public string PhysicalPath
        {
            get
            {
                if (System.IO.Directory.Exists("temp") == false)
                    System.IO.Directory.CreateDirectory("temp");
                string filename = System.IO.Path.Combine("temp", Name);
                if (System.IO.File.Exists(filename) == false)
                {
                    System.IO.File.WriteAllBytes(filename, data);
                }
                return filename;
            }
        }

        public string Name
        {
            get
            {
                var hash = sha1.ComputeHash(this.data);
                string outhex = "";
                foreach (var h in hash)
                {
                    outhex += h.ToString("x02");
                }
                return outhex;
            }
        }

        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new System.IO.MemoryStream(data);
        }
    }

    public class RpcServer
    {
        System.Collections.Concurrent.ConcurrentDictionary<string, onHttp> mapParser = new System.Collections.Concurrent.ConcurrentDictionary<string, onHttp>();
        async Task ProcessAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var form = await FormData.FromRequest(context.Request);

            onHttp onHttp = null;
            mapParser.TryGetValue(path.ToLower(), out onHttp);
            if (onHttp != null)
            {
                var jsonback = await onHttp(form);

                var res = context.Response;
                res.StatusCode = 200;
                res.Headers["Access-Control-Allow-Origin"] = "*";
                res.Headers["Content-Type"] = "text/plain; charset=UTF-8";
                if (jsonback is string)
                {
                    res.Headers["Content-Type"] = "text/plain; charset=UTF-8";
                    await context.Response.WriteAsync(jsonback as string);
                }
                else if (jsonback is File)
                {
                    res.Headers["Content-Type"] = "application/octet-stream";
                    await context.Response.SendFileAsync(jsonback as File);
                }
                else
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("parse return error:" + path);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("not found:" + path);
            }
        }
        public void AddParser(string path, onHttp onhttp)
        {
            mapParser[path.ToLower()] = onhttp;
        }
        IWebHost server;
        public void Start(IPAddress adress, int port)
        {
            this.server = new WebHostBuilder().UseKestrel
                (options => options.Listen(adress, port, listenOptions =>
            {
            }))
            .Configure(app => app.Run(ProcessAsync))
            .Build();
            server.Start();
        }
        public void Stop()
        {
            server.StopAsync();
        }

    }
}
