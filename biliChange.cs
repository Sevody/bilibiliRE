using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace bilibiliRE
{
    class biliChange
    {
        public byte[] changePage(byte[] buffer, string contentType, string url)
        {
            //处理文本类型
            var encoding = Encoding.UTF8;
            Helper.ContentType ct = Helper.GetContentType(contentType);
            if (ct.IsTextType)
            {
                encoding = Encoding.GetEncoding(ct.Charset);
                var text = encoding.GetString(buffer);
                Helper.ContentType result = Helper.GetContentType(text, true);
                if (!string.IsNullOrWhiteSpace(result.Charset))
                {
                    if (result.Charset != ct.Charset)
                    {
                        encoding = Encoding.GetEncoding(result.Charset);
                        text = encoding.GetString(buffer);
                    }
                }

                text = Replace(text, url);
                buffer = encoding.GetBytes(text);

            }
            System.Diagnostics.Debug.WriteLineIf(!ct.IsTextType, url + " 不是文本类型，直接返回");
            return buffer;
        }


        public string Replace(string txt, string url)
        {
            string needUrl = @"http://www.bilibili.com/video/av([\d]*)(.*?)";
            string adRegex = @"<div class=.ad-f.>[\s\S]*?<div class=.player-wrapper.>";
            string reAd = "<div class=\"ad-f\">bilibiliRE233333333</div><div class=\"player-wrapper\">";
            string bofqiRegex = @"<div class=.scontent. id=.bofqi.>[\s\S]*?<!-- Copyright -->[\s\S]*?<div class=.arc-tool-bar.>";
            string rebofqi = "<div class=\"scontent\" id=\"bofqi\"><object type=\"application/x-shockwave-flash\" class=\"player\" data=\"http://static.hdslb.com/play.swf\" id=\"player_placeholder\" style=\"visibility: visible;\"><param name=\"allowfullscreeninteractive\" value=\"true\"><param name=\"allowfullscreen\" value=\"true\"><param name=\"quality\" value=\"high\"><param name=\"allowscriptaccess\" value=\"always\"><param name=\"wmode\" value=\"opaque\"><param name=\"flashvars\" value=\"cid=$1&amp;aid=$2\"></object></div><div class=\"arc-tool-bar\">";

            //如果是播放页
            if (IsMatch(url, needUrl))
            {

                var copyright = Regex.Match(txt, bofqiRegex);
                if (copyright.Success)
                {
                    //替换播放器
                    String aid = Helper.Getaid(url);
                    var cid = Helper.Getcid(aid);
                    rebofqi = rebofqi.Replace("$1", cid);
                    rebofqi = rebofqi.Replace("$2", aid);
                    txt = Regex.Replace(txt, bofqiRegex, rebofqi);
                    return txt;
                }

                //隐藏广告
                txt = Regex.Replace(txt, adRegex, reAd);
            }

            return txt;
        }

        private bool IsMatch(string url, string urlPattern)
        {
            var m = Regex.Match(url, urlPattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                if (m.Index == 0)
                {
                    return true;
                }
            }
            return false;
        }

    }
}

