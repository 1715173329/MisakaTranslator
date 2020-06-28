﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TranslatorLibrary
{
    public class CaiyunTranslator : ITranslator
    {
        public string caiyunToken;//彩云小译 令牌
        private string errorInfo;//错误信息

        public string GetLastError()
        {
            return errorInfo;
        }

        public string Translate(string sourceText, string desLang, string srcLang)
        {
            if (sourceText == "" || desLang == "" || srcLang == "")
            {
                errorInfo = "Param Missing";
                return null;
            }

            if (desLang == "jp")
                desLang = "ja";
            if (srcLang == "jp")
                srcLang = "ja";

            // 原文
            string q = sourceText;
            string retString;

            string trans_type = srcLang + "2" + desLang;

            string url = "https://api.interpreter.caiyunai.com/v1/translator";
            //json参数
            string jsonParam = "{\"source\": [\"" + q + "\"], \"trans_type\": \"" + trans_type + "\", \"request_id\": \"demo\", \"detect\": true}";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("x-authorization", "token " + caiyunToken);
            request.ContentType = "application/json;charset=UTF-8";
            byte[] byteData = Encoding.UTF8.GetBytes(jsonParam);
            int length = byteData.Length;
            request.ContentLength = length;
            try
            {
                Stream writer = request.GetRequestStream();
                writer.Write(byteData, 0, length);
                writer.Close();
                request.UserAgent = null;
                request.Timeout = 6000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (WebException ex)
            {
                errorInfo = ex.Message;
                return null;
            }

            CaiyunTransResult oinfo;
            try
            {
                oinfo = JsonConvert.DeserializeObject<CaiyunTransResult>(retString);
            }
            catch {
                errorInfo = "JsonConvert Error";
                return null;
            }

            if (oinfo != null && oinfo.target.Count >= 1)
            {
                //得到翻译结果
                string r = "";
                for (int i = 0;i < oinfo.target.Count;i++) {
                    r += Regex.Unescape(oinfo.target[i]);
                }

                return r;
            }
            else
            {
                errorInfo = "ErrorInfo:" + oinfo.message;
                return null;
            }
            
        }

        public void TranslatorInit(string param1, string param2 = "")
        {
            caiyunToken = param1;
        }


        /// <summary>
        /// 彩云小译API申请地址
        /// </summary>
        /// <returns></returns>
        public static string GetUrl_allpyAPI()
        {
            return "https://dashboard.caiyunapp.com/user/sign_in/";
        }

        /// <summary>
        /// 彩云小译API额度查询地址
        /// </summary>
        /// <returns></returns>
        public static string GetUrl_bill()
        {
            return "https://dashboard.caiyunapp.com/";
        }

        /// <summary>
        /// 彩云小译API文档地址（错误代码）
        /// </summary>
        /// <returns></returns>
        public static string GetUrl_Doc()
        {
            return "https://fanyi.caiyunapp.com/#/api";
        }
    }


    class CaiyunTransResult
    {
        public string message { get; set; }
        public double confidence { get; set; }
        public int rc { get; set; }
        public List<string> target { get; set; }
    }


}
