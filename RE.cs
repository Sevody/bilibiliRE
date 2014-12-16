using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Configuration;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace bilibiliRE
{
    class RE
    {
        private HttpListenerContext client;       
        private string[] headers;        //pass through headers
        private biliChange change;
        private BiliReborn reborn;       //re

        public RE(HttpListenerContext context)
        {
            this.client = context;
         
            this.change = new biliChange();

            headers = new string[] { "Cookie", "Accept", "Referrer", "Accept-Language" };

            this.reborn = new BiliReborn();
        }


        // gzip或deflate解压
        public byte[] Decompress(byte[] buffer, string contentEncoding)
        {
            if (!string.IsNullOrWhiteSpace(contentEncoding))
            {
                if (contentEncoding == "gzip")
                {
                    var gzip = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress);
                    return Helper.GetBytesFromStream(gzip);
                }
                else if (contentEncoding == "deflate")
                {
                    var deflate = new DeflateStream(new MemoryStream(buffer), CompressionMode.Decompress);
                    return Helper.GetBytesFromStream(deflate);
                }
            }
            return buffer;
        }

        private void SetCookies(HttpWebRequest request)
        {
            try
            {
                var host = new Uri(request.RequestUri.ToString()).Host;
                var index = host.IndexOf('.');
                var domain = host.Substring(index);
                request.CookieContainer = new CookieContainer();
                for (int i = 0; i < client.Request.Cookies.Count; i++)
                {
                    var c = client.Request.Cookies[i];
                    c.Domain = domain;
                    request.CookieContainer.Add(c);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        //主流程
        public void ProcessRequest()
        {
            string url = client.Request.Url.ToString();
            string msg = DateTime.Now.ToString("hh:mm:ss") + " " + client.Request.HttpMethod + " " + url;
            Console.WriteLine(msg);

            //如果是视频播放页
            var videoRegex = @"www.bilibili.com/video/av([\d]*?)";
            var m_v = Regex.Match(url, videoRegex);

            byte[] result;
            try
            {
                
                var request = WebRequest.Create(url) as HttpWebRequest;

                SetCookies(request);
                request.UserAgent = client.Request.UserAgent;
                request.Method = client.Request.HttpMethod;
                request.ContentType = client.Request.ContentType;
                request.ContentLength = client.Request.ContentLength64;
                if (client.Request.ContentLength64 > 0 && client.Request.HasEntityBody)
                {
                    using (System.IO.Stream body = client.Request.InputStream)
                    {
                        byte[] requestdata = Helper.GetBytesFromStream(body);
                        request.ContentLength = requestdata.Length;
                        Stream s = request.GetRequestStream();
                        s.Write(requestdata, 0, requestdata.Length);
                        s.Close();
                    }
                }
                //request processing
                WebResponse response = request.GetResponse() as HttpWebResponse;

                //如果发生重定向则判断是否是土豆或乐视源
                if (response.ResponseUri != null && m_v.Success)
                {                    
                        reborn.BirthPage(response.ResponseUri.ToString());   //re                    
                }

                result = Helper.GetBytesFromStream(response.GetResponseStream());
                client.Response.ContentType = response.ContentType;
                client.Response.AppendHeader("Set-Cookie", response.Headers.Get("Set-Cookie"));
                var contentEncoding = (response.Headers["Content-Encoding"] ?? "").Trim().ToLower();//压缩类型
                result = Decompress(result, contentEncoding);
                response.Close();

            }
            catch (WebException wex)
            {
                result = Encoding.UTF8.GetBytes(wex.Message);
                HttpWebResponse resp = (HttpWebResponse)wex.Response;
                client.Response.StatusCode = (int)resp.StatusCode;
                //re....
               
                if (client.Response.StatusCode == 404 && m_v.Success)
                {
                    //re爱奇艺源页面
                    Helper.Animate.videoSource = "aiqyi";
                    reborn.NeelReborn = true;
                    client.Response.ContentType = @"text/html";

                }
                else
                {
                    client.Response.StatusDescription = resp.StatusDescription;
                    Console.WriteLine("ERROR:" + wex.Message);
                }
            }
            catch (Exception ex)
            {
                result = Encoding.UTF8.GetBytes(ex.Message);
                Console.WriteLine("ERROR:" + ex.Message);
            }
            try
            {
                //response
                byte[] buffer = result;
                if (reborn.NeelReborn)
                {
                    //re土豆和乐视源页面
                    buffer = reborn.rebornPage(buffer, client.Response.ContentType,url);
                }
                else
                {
                    //处理广告和替换播放器
                    buffer = change.changePage(buffer, client.Response.ContentType, url);
                }
                if (Helper.Animate.videoSource == "letv")
                {
                    //re乐视源的视频地址
                    var urlRegex = @"http://interface.bilibili.com/playurl(.*?)player=1";
                    var m_url = Regex.Match(url, urlRegex, RegexOptions.IgnoreCase);
                    if (m_url.Success)
                    {
                        buffer = reborn.rebornCDATA(buffer, client.Response.ContentType, url);
                        Helper.Animate.videoSource = "";
                    }
                }
                client.Response.ContentLength64 = buffer.Length;
                client.Response.OutputStream.Write(buffer, 0, buffer.Length);
                client.Response.OutputStream.Close();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

        }
    }
}
