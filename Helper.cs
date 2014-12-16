using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Net;


namespace bilibiliRE
{
    class Helper
    {
        //记录Animate的视频源
        public static class Animate
        {
            public static string videoSource { get; set; }

            public static string vid { get; set; }
        }


        public class ContentType
        {
            public string Charset { get; set; }
            public string TypeName { get; set; }
            public string SubTypeName { get; set; }
  
            /// 是不是文本类型
            public bool IsTextType { get; set; }
        }

        public static ContentType GetContentType(string txt, bool isHtml = false)
        {
            var result = new ContentType { IsTextType = false };
            //text/html; charset=utf-8
            //application/x-javascript
            var contentTypeRegex = @"\s*?(?<TypeName>[a-zA-Z0-9\\-]+?)/(?<SubTypeName>[a-zA-Z0-9\\-]+);\s*?charset\s*?=\s*?(?<charset>[a-zA-Z0-9\\-]+)";
            if (!string.IsNullOrWhiteSpace(txt))
            {
                var match = Regex.Match(txt, contentTypeRegex, RegexOptions.IgnoreCase);
                if (!match.Success && !isHtml)
                {
                    contentTypeRegex = @"\s*?(?<TypeName>[a-zA-Z0-9\\-]+?)/(?<SubTypeName>[a-zA-Z0-9\\-]+)(;\s*?charset\s*?=\s*?(?<charset>[a-zA-Z0-9\\-]+))?";
                    match = Regex.Match(txt, contentTypeRegex, RegexOptions.IgnoreCase);
                }
                if (match.Success)
                {
                    result.TypeName = match.Groups["TypeName"].Value.Trim().ToLower();
                    result.SubTypeName = match.Groups["SubTypeName"].Value.Trim().ToLower();
                    result.Charset = match.Groups["charset"].Value;
                    if (string.IsNullOrWhiteSpace(result.Charset))
                    {
                        result.Charset = "utf-8";
                    }
                    result.Charset = result.Charset.Trim().ToLower();
                    if (result.TypeName == "text")
                    {
                        result.IsTextType = true;
                    }
                    else if (result.TypeName == "application" && (result.SubTypeName.Contains("json") || result.SubTypeName.Contains("javascript")))
                    {
                        result.IsTextType = true;
                    }
                }
            }
            return result;
        }

        public static byte[] GetBytesFromStream(Stream stream)
        {
            byte[] result;
            byte[] buffer = new byte[256];

            BinaryReader reader = new BinaryReader(stream);
            MemoryStream memoryStream = new MemoryStream();

            int count = 0;
            while (true)
            {
                count = reader.Read(buffer, 0, buffer.Length);
                memoryStream.Write(buffer, 0, count);

                if (count == 0)
                    break;
            }

            result = memoryStream.ToArray();
            memoryStream.Close();
            reader.Close();
            stream.Close();

            return result;
        }

        
        //http请求
        public static byte[] GetHttp(string url)
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            WebResponse response = request.GetResponse() as HttpWebResponse;
            byte[] result = GetBytesFromStream(response.GetResponseStream());
            response.Close();
            return result;
        }

        //根据url获得aid
        public static string Getaid(string url)
        {
            var aidRegex = @"www.bilibili.com/video/av([\d]*)(.*)";
            var m = Regex.Match(url, aidRegex);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            return "1";
        }

        //通过bilibili的html5接口获取cid
        public static string Getcid(string aid)
        {
            String html5Url = "http://www.bilibili.com/m/html5?aid=" + aid;
            byte[] result = GetHttp(html5Url);

            var encoding = Encoding.GetEncoding("utf-8");
            var text = encoding.GetString(result);

            var cidRegex = @"comment.bilibili.com/([\d]*)(.*)";
            var m = Regex.Match(text, cidRegex);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            else
            {
                return "1";
            }
        }

    }
}
