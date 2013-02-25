using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScrapySharp.Network;

namespace YPS.Engine.Core.Tools
{
    public static class HtmlUtil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string EncodeQueryStringSegment(string query)
        {
            return query.ToLower()
                        .Trim()
                        .Replace("&", "%26")
                        .Replace(" ", "%20");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetPageDocument(string site)
        {
            return GetPageDocument(new Uri(site));
        }
        public static string GetPageDocument(Uri site)
        {
            var browser = new ScrapySharp.Network.ScrapingBrowser()
            {
                UserAgent = new FakeUserAgent("compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0", "Mozilla/5.0"),
            };
            return browser.DownloadString(site);

            //   var browser = new ScrapingBrowser();
            //   return browser.DownloadString(new Uri(site));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="element"></param>
        /// <param name="attribute"></param>
        /// <param name="valuePortion"></param>
        /// <returns></returns>
        public static string GetInnerText(HtmlAgilityPack.HtmlNode node,
                                            string element,
                                            string attribute,
                                            string valuePortion,
                                            string defaultValue = "")
        {
            var _node = node.Descendants(element)
                        .Where(d => d.Attributes.Contains(attribute) && d.Attributes[attribute].Value.Contains(valuePortion))
                        .FirstOrDefault();
            if(_node == null)
                return defaultValue;
            return _node.InnerText;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="element"></param>
        /// <param name="attribute"></param>
        /// <param name="valuePortion"></param>
        /// <returns></returns>
        public static HtmlAgilityPack.HtmlNode GetNode(HtmlAgilityPack.HtmlNode node,
                                            string element,
                                            string attribute,
                                            string valuePortion,
                                            bool tryNull = false)
        {
            if(tryNull)
            {
                try
                {
                    var _nodes = node.Descendants(element)
                            .Where(d => d.Attributes.Contains(attribute) && d.Attributes[attribute].Value.Contains(valuePortion));

                    if(_nodes != null && _nodes.Count() > 0)
                    {
                        var _node = _nodes.ToArray()[0];//.SingleOrDefault();
                        return _node;
                    }
                }
                catch { }
            }
            else
            {
                var _nodes = node.Descendants(element)
                            .Where(d => d.Attributes.Contains(attribute) && d.Attributes[attribute].Value.Contains(valuePortion));

                if(_nodes != null && _nodes.Count() > 0)
                {
                    var _node = _nodes.ToArray()[0];//.SingleOrDefault();
                    return _node;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="element"></param>
        /// <param name="attribute"></param>
        /// <param name="valuePortion"></param>
        /// <returns></returns>
        public static IEnumerable<HtmlAgilityPack.HtmlNode> GetNodeCollection(HtmlAgilityPack.HtmlNode node,
                                            string element,
                                            string attribute,
                                            string valuePortion)
        {
            var _nodes = node.Descendants(element)
                        .Where(d => d.Attributes.Contains(attribute) && d.Attributes[attribute].Value.Contains(valuePortion));
            return _nodes;
        }


    }
}
