using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Configuration;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace bilibiliRE
{

    public class Program
    {
        static void Main()
        {
            //添加http监听
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://127.255.255.253/");
            listener.Start();
            Console.WriteLine("Listening on " + "127.255.255.253");

            //多线程处理
            while (true)
            {          
                HttpListenerContext hlc = listener.GetContext();
                Task.Factory.StartNew(new RE(hlc).ProcessRequest);
            }
        }
    }    

}
