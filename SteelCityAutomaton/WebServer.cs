using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;

namespace SimpleWebServer
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");



            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);
            //_listener.Prefixes.Add("http://+:1759/");
            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running...");
                try{
                    while (_listener.IsListening){
                        ThreadPool.QueueUserWorkItem((c) =>{
                            var ctx = c as HttpListenerContext;
                            try{
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.ContentEncoding = Encoding.UTF8;
                                if(ctx.Request.HttpMethod == "POST")ctx.Response.ContentType = "application/json";
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch(Exception e) {
                                Console.WriteLine("[{0}]:{1}:{2}","WS-1",e.Message,e.StackTrace);
                            } // suppress any exceptions
                            finally{
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception e){
                    Console.WriteLine("[{0}]:{1}:{2}", "WS-2", e.Message, e.StackTrace);
                } // suppress any exceptions
            });
        }

        public void Stop(){
            _listener.Stop();
            _listener.Close();
        }
    }
}