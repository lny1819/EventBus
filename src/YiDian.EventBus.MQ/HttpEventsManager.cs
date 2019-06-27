using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// post reg_class?app=a&version=1.0 (ClassMeta)
    /// post reg_enum?app=a&version=1.0 (EnumMeta)
    /// get check?app=a&version=1.0
    /// get version?app=a
    /// get listevent?app=a
    /// get eventid?name=zs
    /// get allids?app=a
    /// get check_not_event?app=a&version=1.0 (true,false)
    /// </summary>
    public class HttpEventsManager : IAppEventsManager
    {
        readonly Uri web_host;
        public HttpEventsManager(string web_api_address)
        {
            var flag = Uri.TryCreate(web_api_address, UriKind.Absolute, out web_host);
            if (!flag) throw new ArgumentException("not vaild web api address", nameof(web_api_address));
        }

        private CheckResult SendTypeMeta(Type type, string appName, string version)
        {
            if (type.IsEnum) return SendEnumMeta(type, appName, version);
            else return SendClassMeta(type, appName, version);
        }

        private CheckResult SendClassMeta(Type type, string appName, string version)
        {
            var isEventType = type.GetInterfaces().Where(x => x == typeof(IMQEvent)).Count() > 0;
            var meta = new ClassMeta() { Name = type.Name, IsEventType = isEventType };
            var list = new List<Type>();
            foreach (var p in type.GetProperties())
            {
                if (isEventType && (p.Name == "ErrorCode" || p.Name == "ErrorMsg")) continue;
                var pinfo = new PropertyMetaInfo() { Name = p.Name };
                if (p.PropertyType == typeof(Int16) || p.PropertyType == typeof(Int32)) pinfo.Type = PropertyMetaInfo.P_Int32;
                else if (p.PropertyType == typeof(Int64)) pinfo.Type = PropertyMetaInfo.P_Int64;
                else if (p.PropertyType == typeof(Boolean)) pinfo.Type = PropertyMetaInfo.P_Boolean;
                else if (p.PropertyType == typeof(string)) pinfo.Type = PropertyMetaInfo.P_String;
                else if (p.PropertyType == typeof(UInt32) || p.PropertyType == typeof(UInt16)) pinfo.Type = PropertyMetaInfo.P_UInt32;
                else if (p.PropertyType == typeof(UInt64)) pinfo.Type = PropertyMetaInfo.P_UInt64;
                else if (p.PropertyType == typeof(Int64)) pinfo.Type = PropertyMetaInfo.P_Int64;
                else if (p.PropertyType == typeof(Double) || p.PropertyType == typeof(Decimal)) pinfo.Type = PropertyMetaInfo.P_Double;
                else if (p.PropertyType == typeof(DateTime)) pinfo.Type = PropertyMetaInfo.P_Date;
                else if (p.PropertyType.IsGenericType && p.PropertyType.GetInterfaces().Contains(typeof(IList)))
                {
                    var s_list = "list#";
                    var t_args = p.PropertyType.GenericTypeArguments;
                    for (var i = 0; i < t_args.Length; i++)
                    {
                        s_list += t_args[i].Name;
                        if (i != t_args.Length - 1) s_list += "#";
                    }
                    pinfo.Type = s_list;
                }
                else if (p.PropertyType.IsArray) pinfo.Type = p.PropertyType.Name;
                else
                {
                    if (!p.PropertyType.IsSubclassOf(typeof(IMQEvent))) list.Add(p.PropertyType);
                    pinfo.Type = p.PropertyType.Name;
                }
                var attrs = p.GetCustomAttributes(typeof(KeyIndexAttribute), false);
                if (attrs.Length != 0) pinfo.Attr = new MetaAttr() { AttrType = AttrType.Index, Value = ((KeyIndexAttribute)attrs[0]).Index.ToString() };
                meta.Properties.Add(pinfo);
            }
            var res = RegisterClassEvent(appName, version, meta);
            foreach (var not_event_type in list)
            {
                if (!IfExistNotEventType(appName, not_event_type, version))
                {
                    res = SendTypeMeta(not_event_type, appName, version);
                    if (!res.IsVaild) return res;
                }
            }
            return res;
        }

        private CheckResult SendEnumMeta(Type type, string appName, string version)
        {
            var enumMeta = new EnumMeta() { Name = type.Name };
            var values = Enum.GetValues(type);
            foreach (var v in values)
            {
                enumMeta.Values.Add((v.ToString(), (int)v));
            }
            return RegisterEnumType(appName, version, enumMeta);
        }

        public CheckResult RegisterEvent<T>(string appName, string version) where T : IMQEvent
        {
            return SendTypeMeta(typeof(T), appName, version);
        }
        //public void RegisterEvents(AppMetas metas)
        //{
        //    var app = metas.Name;
        //    var version = metas.Version;
        //    foreach (var meta in metas.MetaInfos)
        //    {
        //        RegisterEvent(app, version, meta);
        //    }
        //}

        #region HttpApi
        private CheckResult RegisterClassEvent(string appname, string version, ClassMeta meta)
        {
            try
            {
                var uri = "reg_class?app=" + appname + "&version=" + version;
                var sb = new StringBuilder();
                meta.ToJson(sb);
                var json = sb.ToString();
                var response = PostReq(uri, json);
                var obj = JsonString.Unpack(response);
                var ht = (Hashtable)obj;
                var res = new CheckResult
                {
                    IsVaild = (bool)ht["IsVaild"],
                    InvaildMessage = (ht["InvaildMessage"] ?? "").ToString(),
                };
                return res;
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        private CheckResult RegisterEnumType(string appname, string version, EnumMeta meta)
        {
            try
            {
                var uri = "reg_enum?app=" + appname + "&version=" + version;
                var sb = new StringBuilder();
                meta.ToJson(sb);
                var json = sb.ToString();
                var response = PostReq(uri, json);
                var obj = JsonString.Unpack(response);
                var ht = (Hashtable)obj;
                var res = new CheckResult
                {
                    IsVaild = (bool)ht["IsVaild"],
                    InvaildMessage = (ht["InvaildMessage"] ?? "").ToString(),
                };
                return res;
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        public CheckResult VaildityTest(string appname, string version)
        {
            try
            {
                var uri = "check?app=" + appname + "&version=" + version;
                var response = GetReq(uri);
                var obj = JsonString.Unpack(response);
                var ht = (Hashtable)obj;
                var res = new CheckResult
                {
                    IsVaild = bool.Parse(ht["IsVaild"].ToString()),
                    InvaildMessage = ht["InvaildMessage"].ToString(),
                };
                return res;
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        public string GetVersion(string appname)
        {
            var uri = "version?app=" + appname;
            var value = GetReq(uri);
            return value;
        }
        public AppMetas ListEvents(string appname)
        {
            var uri = "listevent?app=" + appname;
            var value = GetReq(uri);
            return ToMetas(value);
        }
        public bool IfExistNotEventType(string appName, Type type, string version)
        {
            var typename = type.Name;
            var uri = "check_not_event?app=" + appName + "&name=" + typename;
            var value = GetReq(uri);
            bool.TryParse(value, out bool res);
            return res;
        }

        public CheckResult GetEventId(string typename)
        {
            try
            {
                var uri = "eventid?name=" + typename;
                var response = GetReq(uri);
                var obj = JsonString.Unpack(response);
                var ht = (Hashtable)obj;
                var res = new CheckResult
                {
                    IsVaild = (bool)ht["IsVaild"],
                    InvaildMessage = (ht["InvaildMessage"] ?? "").ToString(),
                };
                return res;
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        public List<EventId> GetEventIds(string appname)
        {
            var uri = "allids?app=" + appname;
            var value = GetReq(uri);
            var obj = JsonString.Unpack(value);
            if (obj == null || obj.GetType() != typeof(ArrayList)) throw new ArgumentException("the returns is not expected result");
            var al = (ArrayList)obj;
            var list = new List<EventId>();
            foreach (var item in al)
            {
                var ht = (Hashtable)item;
                list.Add(new EventId() { ID = ht["ID"].ToString(), Name = ht["Name"].ToString() });
            }
            return list;
        }
        #endregion
        AppMetas ToMetas(string json)
        {
            var obj = JsonString.Unpack(json);
            if (obj == null || obj.GetType() != typeof(Hashtable)) throw new ArgumentException("the returns is not expected result");
            var ht = (Hashtable)obj;
            var appmetas = new AppMetas
            {
                Name = ht["Name"].ToString(),
                Version = ht["Version"].ToString(),
            };
            var list = (ArrayList)ht["MetaInfos"];
            foreach (var item in list)
            {
                var ht2 = (Hashtable)item;
                var class_meta = new ClassMeta
                {
                    Name = ht2["Name"].ToString(),
                    IsEventType = bool.Parse(ht2["IsEventType"].ToString())
                };
                if (ht2["Attr"] != null)
                {
                    var attr_ht = (Hashtable)ht2["Attr"];
                    var attr = new MetaAttr()
                    {
                        AttrType = (AttrType)(int.Parse(attr_ht["AttrType"].ToString())),
                        Value = attr_ht["Value"].ToString()
                    };
                    class_meta.Attr = attr;
                }
                var pss_ht = (ArrayList)ht2["Properties"];
                foreach (Hashtable ps in pss_ht)
                {
                    var p_info = new PropertyMetaInfo()
                    {
                        Name = ps["Name"].ToString(),
                        Type = ps["Type"].ToString()
                    };
                    if (ps["Attr"] != null)
                    {
                        var p_attr_ht = (Hashtable)ps["Attr"];
                        var p_attr = new MetaAttr()
                        {
                            AttrType = (AttrType)(int.Parse(p_attr_ht["AttrType"].ToString())),
                            Value = p_attr_ht["Value"].ToString()
                        };
                        p_info.Attr = p_attr;
                    }
                    class_meta.Properties.Add(p_info);
                }
                appmetas.MetaInfos.Add(class_meta);
            }
            var list2 = (ArrayList)ht["Enums"];
            foreach (var item in list2)
            {
                var ht2 = (Hashtable)item;
                var v_ht = (ArrayList)ht2["Values"];
                var enumMeta = new EnumMeta() { Name = ht2["Name"].ToString() };
                foreach (var v in v_ht)
                {
                    var h_v = (Hashtable)v;
                    enumMeta.Values.Add((h_v["Item1"].ToString(), int.Parse(h_v["Item2"].ToString())));
                }
                appmetas.Enums.Add(enumMeta);
            }
            return appmetas;
        }
        string PostReq(string uri, string value)
        {
            uri = web_host.OriginalString + "/" + uri;
            if (web_host.Scheme == Uri.UriSchemeHttps) return HttpsPost(uri, value);
            return HttpPost(uri, value);
        }
        string GetReq(string uri)
        {
            uri = web_host.OriginalString + "/" + uri;
            if (web_host.Scheme == Uri.UriSchemeHttps) return HttpGet(uri);
            return HttpGet(uri);
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
            var new_stream = webreq.GetResponse().GetResponseStream();
            StreamReader reader = new StreamReader(new_stream, Encoding.UTF8);
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
