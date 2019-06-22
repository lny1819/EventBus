using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// reg?app=a&version=1.0
    /// check?app=a&version=1.0
    /// </summary>
    public class HttpEventsManager : IAppEventsManager
    {
        //version?app=a
        //listevent?app=a
        //eventid?app=a&name=zs
        //allids?app=a
        readonly Uri web_host;
        public HttpEventsManager(string web_api_address)
        {
            var flag = Uri.TryCreate(web_api_address, UriKind.Absolute, out web_host);
            if (!flag) throw new ArgumentException("not vaild web api address", nameof(web_api_address));
        }
        public AppMetas GetAppEventTypes(string appName, string version = "")
        {
            throw new NotImplementedException();
        }

        public string GetEventID<T>(string appName, string version)
        {
            throw new NotImplementedException();
        }

        public void RegisterEvent<T>(string appName, string version) where T : IntegrationMQEvent
        {
            var type = typeof(T);
            SendMeta(type, appName, version);
        }
        private void SendMeta(Type type, string appName, string version)
        {
            var meta = new ClassMeta()
            {
                Name = type.Name
            };
            foreach (var p in type.GetProperties())
            {
                var pinfo = new PropertyMetaInfo() { Name = p.Name };
                if (p.PropertyType == typeof(Int16) || p.PropertyType == typeof(Int32)) pinfo.Type = PropertyMetaInfo.P_Int32;
                else if (p.PropertyType == typeof(Int64)) pinfo.Type = PropertyMetaInfo.P_Int64;
                else if (p.PropertyType == typeof(Boolean)) pinfo.Type = PropertyMetaInfo.P_Boolean;
                else if (p.PropertyType == typeof(string)) pinfo.Type = PropertyMetaInfo.P_String;
                else if (p.PropertyType == typeof(UInt32) || p.PropertyType == typeof(UInt16)) pinfo.Type = PropertyMetaInfo.P_UInt32;
                else if (p.PropertyType == typeof(UInt64)) pinfo.Type = PropertyMetaInfo.P_UInt64;
                else if (p.PropertyType == typeof(Int64)) pinfo.Type = PropertyMetaInfo.P_Int64;
                else if (p.PropertyType == typeof(Double) || p.PropertyType == typeof(Decimal)) pinfo.Type = PropertyMetaInfo.P_Double;
                else pinfo.Type = p.PropertyType.Name;
                var attrs = p.GetCustomAttributes(typeof(KeyIndexAttribute), false);
                if (attrs.Length != 0) pinfo.Attr = new MetaAttr() { AttrType = AttrType.Index, Value = ((KeyIndexAttribute)attrs[0]).Index.ToString() };
                meta.Properties.Add(pinfo);
            }
            RegisterEvent(appName, version, meta);
        }
        public void RegisterEvents(AppMetas metas)
        {
            var app = metas.Name;
            var version = metas.Version;
            foreach (var meta in metas.MetaInfos)
            {
                RegisterEvent(app, version, meta);
            }
        }
        public CheckResult VaildityTest(string appName, string version)
        {
            throw new NotImplementedException();
        }

        #region HttpApi
        private void RegisterEvent(string appname, string version, ClassMeta meta)
        {
            var uri = "reg?app=" + appname + "&version=" + version;
            var sb = new StringBuilder();
            meta.ToJson(sb);
            var json = sb.ToString();
            Req(uri, json);
        }
        private bool Check(string appname, string version)
        {
            var uri = "check?app=" + appname + "&version=" + version;
            var value = HttpGet(uri);
            bool.TryParse(value, out bool res);
            return res;
        }
        private string GetVersion(string appname)
        {
            var uri = "version?app=" + appname;
            var value = HttpGet(uri);
            return value;
        }
        public AppMetas ListEvents(string appname)
        {
            var uri = "listevent?app=" + appname;
            var value = HttpGet(uri);
            return ToMetas(value);
        }
        #endregion
        public AppMetas ToMetas(string json)
        {
            var obj = JsonString.Unpack(json);
            if (obj == null) throw new ArgumentException("the returns is not expected result");
            var ht = (Hashtable)obj;
            var appmetas = new AppMetas
            {
                Name = ht["Name"].ToString(),
                Version = ht["Version"].ToString()
            };
            var list = (ArrayList)ht["MetaInfos"];
            foreach (var item in list)
            {
                var ht2 = (Hashtable)item;
                var class_meta = new ClassMeta
                {
                    Name = ht2["Name"].ToString()
                };
                var attr_ht = (Hashtable)ht2["Attr"];
                var attr = new MetaAttr()
                {
                    AttrType = (AttrType)(int.Parse(attr_ht["AttrType"].ToString())),
                    Value = attr_ht["Value"].ToString()
                };
                class_meta.Attr = attr;
                var pss_ht = (ArrayList)ht2["Properties"];
                foreach (Hashtable ps in pss_ht)
                {
                    var p_info = new PropertyMetaInfo()
                    {
                        Name = ps["Name"].ToString(),
                        Type = ps["Type"].ToString()
                    };
                    var p_attr_ht = (Hashtable)ps["Attr"];
                    var p_attr = new MetaAttr()
                    {
                        AttrType = (AttrType)(int.Parse(p_attr_ht["AttrType"].ToString())),
                        Value = p_attr_ht["Value"].ToString()
                    };
                    p_info.Attr = p_attr;
                    class_meta.Properties.Add(p_info);
                }
                appmetas.MetaInfos.Add(class_meta);
            }
            return appmetas;
        }
        string Req(string uri, string value)
        {
            uri = web_host.OriginalString + "/" + uri;
            if (web_host.Scheme == Uri.UriSchemeHttps) return HttpsPost(uri, value);
            return HttpPost(uri, value);
        }
        string HttpsPost(string url, string value)
        {
            WebRequest request = null;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            request = WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("charset", "utf-8");
            request.ContentType = "application/json";
            var encode = Encoding.UTF8;
            byte[] data = encode.GetBytes(value);
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var resp = request.GetResponse();
            var respS = resp.GetResponseStream();
            StreamReader reader = new StreamReader(respS);
            var res = reader.ReadToEnd();
            reader.Close();
            resp.Close();
            respS.Close();
            return res;
        }
        string HttpPost(string url, string value)
        {
            var webreq = WebRequest.Create(url);
            webreq.Method = "POST";
            webreq.ContentType = "application/json";
            var stream = webreq.GetRequestStream();
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
            stream = webreq.GetResponse().GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        public string HttpGet(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("charset", "utf-8");
            var resp = request.GetResponse();
            var respS = resp.GetResponseStream();
            StreamReader reader = new StreamReader(respS);
            var value = reader.ReadToEnd();
            reader.Close();
            resp.Close();
            respS.Close();
            return value;
        }
        private bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
