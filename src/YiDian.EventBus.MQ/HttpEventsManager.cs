using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using YiDian.EventBus.MQ.KeyAttribute;

namespace YiDian.EventBus.MQ
{
    /*
    /// post reg_class?app=a&version=1.0 (ClassMeta)
    /// post reg_enum?app=a&version=1.0 (EnumMeta)
    /// get check?app=a&version=1.0
    /// get version?app=a
    /// get listevent?app=a
    /// get eventid?name=zs
    /// get allids?app=a
    /// get check_not_event?app=a&version=1.0 (true,false)
    */
    /// <summary>
    /// 基于webapi的MQ事件名称管理器
    /// </summary>
    public class HttpEventsManager : IAppEventsManager
    {
        const char separator = '#';
        readonly Uri web_host;
        /// <summary>
        /// 是否允许未注册的事件
        /// </summary>
        public bool AllowNoRegisterEvent { get; }
        /// <summary>
        /// 创建一个事件管理器
        /// </summary>
        /// <param name="web_api_address"></param>
        /// <param name="allow_no_reg"></param>
        public HttpEventsManager(string web_api_address, bool allow_no_reg = false)
        {
            AllowNoRegisterEvent = allow_no_reg;
            var flag = Uri.TryCreate(web_api_address, UriKind.Absolute, out web_host);
            if (!flag) throw new ArgumentException("not vaild web api address", nameof(web_api_address));
        }

        private CheckResult SendTypeMeta(Type type, string appName, string version, bool enableDefaultSeralize)
        {
            if (type.IsEnum) return SendEnumMeta(type, appName, version, enableDefaultSeralize);
            else return SendClassMeta(type, appName, version, enableDefaultSeralize);
        }

        private CheckResult SendClassMeta(Type type, string appName, string version, bool enableDefaultSeralize)
        {
            var meta = CreateClassMeta(type, appName, out List<Type> list, enableDefaultSeralize);
            var res = RegisterClassEvent(appName, version, meta);
            foreach (var not_event_type in list)
            {
                res = IfExistNotEventType(appName, not_event_type, version);
                if (!res.IsVaild)
                {
                    SendTypeMeta(not_event_type, appName, version, enableDefaultSeralize);
                }
            }
            return res;
        }
        ClassMeta CreateClassMeta(Type type, string appName, out List<Type> types, bool enableDefaultSeralize)
        {
            var isEventType = type.GetInterfaces().Where(x => x == typeof(IMQEvent)).Count() > 0;
            var meta = new ClassMeta() { Name = type.Name, IsEventType = isEventType, DefaultSeralize = enableDefaultSeralize };

            types = new List<Type>();
            foreach (var p in type.GetProperties())
            {
                var pinfo = new PropertyMetaInfo() { Name = p.Name, Type = GetBaseTypeName(p.PropertyType.Name), SeralizeIndex = ((SeralizeIndex)p.GetCustomAttribute(typeof(SeralizeIndex), false)).Index };
                if (pinfo.Type == string.Empty)
                {
                    if (p.PropertyType.IsGenericType && p.PropertyType.GetInterfaces().Contains(typeof(IList)))
                    {
                        var s_list = PropertyMetaInfo.P_Array + separator;
                        var t_args = p.PropertyType.GenericTypeArguments;
                        for (var i = 0; i < t_args.Length; i++)
                        {
                            var pname = GetBaseTypeName(t_args[i].Name);
                            if (string.IsNullOrEmpty(pname)) pname = t_args[i].Name;
                            s_list += pname;
                            if (i != t_args.Length - 1) s_list += separator;
                        }
                        pinfo.Type = s_list;
                    }
                    else if (p.PropertyType.IsArray)
                    {
                        var pname = p.PropertyType.Name;
                        pname = pname.Substring(0, pname.IndexOf('['));
                        var name = GetBaseTypeName(pname);
                        if (string.IsNullOrEmpty(name)) name = pname;
                        pinfo.Type = PropertyMetaInfo.P_Array + separator + name;
                    }
                    else
                    {
                        if (p.PropertyType.IsEnum) pinfo.Type = PropertyMetaInfo.P_Enum + separator + p.PropertyType.Name;
                        else pinfo.Type = p.PropertyType.Name;
                        if (!p.PropertyType.IsSubclassOf(typeof(IMQEvent))) types.Add(p.PropertyType);
                    }
                }
                var attrs = p.GetCustomAttributes(typeof(KeyIndex), false);
                if (attrs.Length != 0) pinfo.Attr = new MetaAttr() { AttrType = AttrType.Index, Value = ((KeyIndex)attrs[0]).Index.ToString() };
                meta.Properties.Add(pinfo);
            }

            return meta;
        }
        private string GetBaseTypeName(string typename)
        {
            if (typename == typeof(Byte).Name) return PropertyMetaInfo.P_Byte;
            else if (typename == typeof(Int16).Name) return PropertyMetaInfo.P_Int16;
            else if (typename == typeof(UInt16).Name) return PropertyMetaInfo.P_UInt16;
            else if (typename == typeof(Int32).Name) return PropertyMetaInfo.P_Int32;
            else if (typename == typeof(UInt32).Name) return PropertyMetaInfo.P_UInt32;
            else if (typename == typeof(Int64).Name) return PropertyMetaInfo.P_Int64;
            else if (typename == typeof(UInt64).Name) return PropertyMetaInfo.P_UInt64;
            else if (typename == typeof(Boolean).Name) return PropertyMetaInfo.P_Boolean;
            else if (typename == typeof(string).Name || typename == typeof(char).Name) return PropertyMetaInfo.P_String;
            else if (typename == typeof(Double).Name || typename == typeof(Decimal).Name) return PropertyMetaInfo.P_Double;
            else if (typename == typeof(DateTime).Name) return PropertyMetaInfo.P_Date;
            else return string.Empty;
        }
        private CheckResult SendEnumMeta(Type type, string appName, string version, bool enableDefaultSeralize)
        {
            var enumMeta = new EnumMeta() { Name = type.Name, DefaultSeralize = enableDefaultSeralize };
            var values = Enum.GetValues(type);
            foreach (var v in values)
            {
                enumMeta.Values.Add((v.ToString(), (int)v));
            }
            return RegisterEnumType(appName, version, enumMeta);
        }

        public CheckResult RegisterEvent<T>(string appName, string version, bool enableDefaultSeralize) where T : IMQEvent
        {
            return SendTypeMeta(typeof(T), appName, version, enableDefaultSeralize);
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
                return response.JsonTo<CheckResult>();
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
                return response.JsonTo<CheckResult>();
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
                return response.JsonTo<CheckResult>();
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        public CheckResult GetVersion(string appname)
        {
            try
            {
                var uri = "version?app=" + appname;
                var version = GetReq(uri);
                var res = new CheckResult
                {
                    IsVaild = true,
                    InvaildMessage = version
                };
                return res;
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }
        public AppMetas ListEvents(string appname)
        {
            var uri = "listevent?app=" + appname;
            var value = GetReq(uri);
            return ToMetas(value);
        }
        public CheckResult IfExistNotEventType(string appName, Type type, string version)
        {
            try
            {
                var typename = type.Name;
                var uri = "check_not_event?app=" + appName + "&name=" + typename + "&version=" + version;
                var response = GetReq(uri);
                return response.JsonTo<CheckResult>();
            }
            catch (Exception ex)
            {
                return new CheckResult() { IsVaild = false, InvaildMessage = ex.ToString() };
            }
        }

        public CheckResult GetEventId(string typename)
        {
            try
            {
                var uri = "eventid?name=" + typename;
                var response = GetReq(uri);
                return response.JsonTo<CheckResult>();
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
            return value.JsonTo<List<EventId>>();
        }
        #endregion
        AppMetas ToMetas(string json)
        {
            return json.JsonTo<AppMetas>();
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
            var res = reader.ReadToEnd();
            reader.Close();
            new_stream.Close();
            return res;
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
