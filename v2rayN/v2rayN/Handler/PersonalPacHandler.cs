using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using v2rayN.Mode;
using v2rayN.Properties;

namespace v2rayN.Handler
{
    class PersonalPacHandler
    {
        private Config _config;
        private HttpListener listener;
        public event EventHandler<string[]> OnPersonalPacUpdated;

        public PersonalPacHandler(Config conf)
        {
            _config = conf;
        }

        public void StartListening()
        {
            listener = new HttpListener();
            var portNumber = _config.GetLocalPort("personalPac");
            var url = Global.TcpHeaderHttp + "://" + Global.Loopback + ":" + portNumber.ToString() + "/";
            //listener.Prefixes.Add(url);
            listener.Start();
            Task.Run(HandleIncomingConnections);
        }

        private async Task HandleIncomingConnections()
        {
            bool runServer = true;
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                resp.Headers.Add("Access-Control-Allow-Origin", "*");
                StreamReader reader = new StreamReader(req.InputStream);
                string content = reader.ReadToEnd();
                UpdatePersonalPac(content);
                byte[] data = Encoding.UTF8.GetBytes("received");
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        private string GetPersonalPacLocation()
        {
            var path = Path.Combine(Environment.CurrentDirectory, Global.PersonalPacLocation);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            else
            {
                var res = Path.Combine(Environment.CurrentDirectory, Global.PersonalPacFileName);
                return res;
            }
        }

        private string[] GetPersonalPacContent(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllLines(path);
            }
            else
            {
                File.Create(path);
                return new string[0];
            }
        }

        private void UpdatePersonalPac(string updates)
        {
            var arr = updates.Split(',');
            var name = GetPersonalPacLocation();
            var old = GetPersonalPacContent(name);
            var diff = arr.Except(old).ToArray();
            var recent = arr.Union(old).ToArray();
            File.WriteAllLines(name, recent);
            UpdatePacFile(recent);
            OnPersonalPacUpdated.Invoke(this, diff);
        }

        private void UpdatePacFile(string[] personalLines)
        {
            var lines = File.ReadAllLines(Global.OriginalPacFileName);
            var newLines = lines.Union(personalLines);
            string abpContent = Utils.UnGzip(Resources.abp_js);
            abpContent = abpContent.Replace("__RULES__", JsonConvert.SerializeObject(newLines, Formatting.Indented));
            File.WriteAllText(Utils.GetPath(Global.pacFILE), abpContent, Encoding.UTF8);
        }

    }
}
