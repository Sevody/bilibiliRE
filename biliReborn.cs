using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

namespace bilibiliRE
{
    public class BiliReborn
    {
        public bool NeelReborn { get; set; }

        public string aid { get; set; }

        public string cid { get; set; }

        public string pid { get; set; }

        public string title { get; set; }

        //是否需要重新生成播放页
        public BiliReborn()
        {
            NeelReborn = false;
        }

        //判断是土豆还是乐视源
        public void BirthPage(String responseUrl)
        {
            var tudouPage = @"cartoon.tudou.com(.*?)#([\d]{1,3})";
            var letvPage = @"comic.letv.com(.*?)#p([\d]{1,3})";
            var m1 = Regex.Match(responseUrl, tudouPage, RegexOptions.IgnoreCase);
            var m2 = Regex.Match(responseUrl, letvPage, RegexOptions.IgnoreCase);
            if (m1.Success)
            {
                Helper.Animate.videoSource = "tudou";
                NeelReborn = true;
                pid = m1.Groups[2].Value;
            }
            else if (m2.Success)
            {
                Helper.Animate.videoSource = "letv";
                NeelReborn = true;
                pid = m2.Groups[2].Value;
            }
        }

        //重新生成播放页
        public byte[] rebornPage(byte[] buffer, string contentType, string url)
        {
            

            this.aid = Helper.Getaid(url);
            this.cid = Helper.Getcid(aid);

            var encoding = Encoding.UTF8;
            Helper.ContentType ct = Helper.GetContentType(contentType, true);
            if (ct.Charset == null)
            {
                ct.Charset = "utf-8";
            }
            if (Helper.Animate.videoSource != "aiqyi")
            {
                encoding = Encoding.GetEncoding(ct.Charset);
                var text = encoding.GetString(buffer);
                Helper.ContentType result = Helper.GetContentType(text, true);
                if (result.Charset != ct.Charset)
                {
                    encoding = Encoding.GetEncoding(result.Charset);
                    text = encoding.GetString(buffer);
                }


                if (Helper.Animate.videoSource == "letv")
                {
                    //获取letv源的vid
                    var videoBoxRegex = @"<div class=.page_box.>([\s\S]*?)</a></div>";
                    var m_vBox = Regex.Match(text, videoBoxRegex, RegexOptions.IgnoreCase);
                    String videoBox = m_vBox.Groups[0].Value;
                    var idRegex = @"vid=.([\d]*?)([\D]{1,4})bili-cid=.([\d]*?)";
                    var m_vidBox = Regex.Matches(videoBox, idRegex);
                    int vpid = int.Parse(pid) - 1;
                    String vidString = m_vidBox[vpid].Value;

                    var vidRegex = @"vid=.([\d]*?)([\D]{1,4})bili-cid=";
                    var m_vid = Regex.Match(vidString, vidRegex, RegexOptions.IgnoreCase);
                    if (m_vid.Success)
                    {
                        Helper.Animate.vid = m_vid.Groups[1].Value;

                    }

                    //获取letv源的title
                    var titleRegex = "<meta name=\"irTitle\" content=\"([^\"]*).";
                    var m_title = Regex.Match(text, titleRegex, RegexOptions.IgnoreCase);
                    if (m_title.Success)
                    {
                        this.title = m_title.Groups[1].Value;
                    }

                }

                if (Helper.Animate.videoSource == "tudou")
                {
                    ct.Charset = "gbk";

                    //获取tudou源的title
                    var titleRegex = "<meta name=\"Keywords\" content=\"([^\"]*).";
                    var m_title = Regex.Match(text, titleRegex, RegexOptions.IgnoreCase);
                    if (m_title.Success)
                    {
                        this.title = m_title.Groups[1].Value;
                    }
                }

            }
 

            Stream myStream = new FileStream("template.html", FileMode.Open);
            Encoding encode = System.Text.Encoding.GetEncoding(ct.Charset);

            StreamReader myStreamReader = new StreamReader(myStream, encode);
            string strinput = myStreamReader.ReadToEnd();
            myStream.Close();


            bofqi = bofqi.Replace("%cid%", cid);
            bofqi = bofqi.Replace("%aid%", aid);
            strinput = strinput.Replace(o_bofqi, bofqi);

            string stroutput = ReplaceVars(strinput); //re...
            myStream.Close();

            buffer = encode.GetBytes(stroutput);

            return buffer;
        }

        //重新生成视频源地址
        public byte[] rebornCDATA(byte[] buffer, string contentType, string url)
        {
            String cdata = GetCDATA(Helper.Animate.vid);

            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            buffer = encode.GetBytes(cdata);
            return buffer;
        }

        //替换模板的变量
        public string ReplaceVars(string template)
        {
            string atitle = title + " " + pid;
            template = template.Replace("%title%", atitle);
            template = template.Replace("%avid%", aid);
            return template; //re...
        }

             
        //获取乐视源视频地址
        public String GetCDATA(string vid)
        {
            String letvUrl1 = "http://api.letv.com/mms/out/video/playJson?id=" + vid + "&platid=1&splatid=101&format=1&tkey=" + letu_() + "&domain=www.letv.com";


            var result1 = Helper.GetHttp(letvUrl1);
            

            var encoding = Encoding.GetEncoding("utf-8");
            String cdata1 = encoding.GetString(result1);

            JObject obj1 = JObject.Parse(cdata1);
            String suffix = "&ctv=pc&termid=0&format=1&hwtype=un&ostype=Windows7&tag=letv&sign=letv&expect=3&tn=&pay=0&rateid=1000";
            String letvUrl2 = ((string)(obj1["playurl"]["domain"][0])) + ((string)(obj1["playurl"]["dispatch"]["720p"][0])) + suffix;
            letvUrl2 = letvUrl2.Replace("tss=ios", "tss=no");

            //720p
            var result2 = Helper.GetHttp(letvUrl2);
            
            String cdata2 = encoding.GetString(result2);
            JObject obj2 = JObject.Parse(cdata2);

            //1080p
            String letvUrl3 = ((string)(obj1["playurl"]["domain"][0])) + ((string)(obj1["playurl"]["dispatch"]["1080p"][0])) + suffix;
            letvUrl3 = letvUrl3.Replace("tss=ios", "tss=no");

            var result3 = Helper.GetHttp(letvUrl3);
           

            String cdata3 = encoding.GetString(result3);
            JObject obj3 = JObject.Parse(cdata3);

            //720p视频源
            String letvCdata0 = (string)(obj2["nodelist"][0]["location"]);
            String letvCdata1 = (string)(obj2["nodelist"][1]["location"]);
            String letvCdata2 = (string)(obj2["nodelist"][2]["location"]);

            //1080p视频源
            String letvCdata3 = (string)(obj3["nodelist"][0]["location"]);
            String letvCdata4 = (string)(obj3["nodelist"][1]["location"]);
            String letvCdata5 = (string)(obj3["nodelist"][2]["location"]);

            String cdataSource = @"<?xml version='1.0' encoding='UTF-8'?><video><result>succ</result><timelength>1420119</timelength><src>30</src><durl><order>1</order><length>1420119</length><url><![CDATA[" + letvCdata3 + "]]></url><backup_url><url><![CDATA[" + letvCdata5 + "]]></url><url><![CDATA[" + letvCdata4 + "]]></url><url><![CDATA[" + letvCdata0 + "]]></url><url><![CDATA[" + letvCdata1 + "]]></url><url><![CDATA[" + letvCdata2 + "]]></url></backup_url></durl></video>";

            return cdataSource;
        }

        //乐视源解析tkey算法
        public long let_(long value, long key)
        {
            var i = 0;
            while (i < key)
            {
                value = 2147483647 & value >> 1 | (value & 1) << 31;
                ++i;
            }
            return value;
        }

        public String letu_()
        {
            DateTime timeStamp = new DateTime(1970, 1, 1);  //得到1970年的时间戳  
            long stime = (DateTime.UtcNow.Ticks - timeStamp.Ticks) / 10000000;

            long key = 773625421;

            long value = let_(stime, key % 13);
            value = value ^ key;
            value = let_(value, key % 17);
            return value.ToString();
        }

        String o_bofqi = "<div class=\"scontent\" id=\"bofqi\"></div>";
        String bofqi = "<div class=\"scontent\" id=\"bofqi\"><object type=\"application/x-shockwave-flash\" class=\"player\" data=\"http://static.hdslb.com/play.swf\" id=\"player_placeholder\" style=\"visibility: visible;\"><param name=\"allowfullscreeninteractive\" value=\"true\"><param name=\"allowfullscreen\" value=\"true\"><param name=\"quality\" value=\"high\"><param name=\"allowscriptaccess\" value=\"always\"><param name=\"wmode\" value=\"opaque\"><param name=\"flashvars\" value=\"cid=%cid%&amp;aid=%aid%\"></object></div>";
       

    }


}
