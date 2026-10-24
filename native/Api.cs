using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace DecodGrmNative {
    public sealed class ApiClient : IDisposable {
        public readonly string BaseUrl;
        readonly JavaScriptSerializer js = new JavaScriptSerializer { MaxJsonLength = 64 * 1024 * 1024 };
        public ApiClient(string baseUrl) { BaseUrl = baseUrl.TrimEnd('/'); }

        public Dictionary<string,object> Get(string path) { return Request("GET", path, null); }
        public Dictionary<string,object> Post(string path, object body) { return Request("POST", path, body ?? new Dictionary<string,object>()); }
        public Dictionary<string,object> Delete(string path) { return Request("DELETE", path, null); }

        Dictionary<string,object> Request(string method, string path, object body) {
            var req = (HttpWebRequest)WebRequest.Create(BaseUrl + path);
            req.Method = method;
            req.Timeout = 30000;
            req.ReadWriteTimeout = 30000;
            req.Accept = "application/json";
            if (body != null) {
                var bytes = Encoding.UTF8.GetBytes(js.Serialize(body));
                req.ContentType = "application/json; charset=utf-8";
                req.ContentLength = bytes.Length;
                using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            }
            try {
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) {
                    var txt = sr.ReadToEnd();
                    return string.IsNullOrWhiteSpace(txt) ? new Dictionary<string,object>() : js.Deserialize<Dictionary<string,object>>(txt);
                }
            } catch (WebException ex) {
                string msg = ex.Message;
                try {
                    using (var resp = ex.Response)
                    using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8)) {
                        var txt = sr.ReadToEnd();
                        var d = js.Deserialize<Dictionary<string,object>>(txt);
                        if (d != null && d.ContainsKey("error")) msg = Convert.ToString(d["error"]);
                    }
                } catch {}
                throw new InvalidOperationException(msg, ex);
            }
        }

        public Dictionary<string,object> Bootstrap() { return Get("/api/bootstrap"); }
        public void Sync() { Post("/api/sync", new Dictionary<string,object>()); }
        public void Login(string pin) { Post("/api/auth/login", new Dictionary<string,object>{{"pin", pin}}); }
        public void SaveEntity(string kind, Dictionary<string,object> item) { Post("/api/entity/" + kind, item); }
        public void DeleteEntity(string kind, string id) { Delete("/api/entity/" + kind + "?id=" + Uri.EscapeDataString(id ?? "")); }
        public Dictionary<string,object> Pairing() { return Post("/api/mobile-pairing", new Dictionary<string,object>()); }
        public void SaveSettings(Dictionary<string,object> settings) { Post("/api/settings", settings); }
        public void ReminderAction(string id, string action) { Post("/api/reminder/action", new Dictionary<string,object>{{"id",id},{"action",action}}); }
        public void RestoreCloud() { Post("/api/cloud/restore-latest", new Dictionary<string,object>()); }
        public void Shutdown() { try { Post("/api/shutdown", new Dictionary<string,object>()); } catch {} }

        public void Download(string path, string target) {
            using (var wc = new WebClient()) wc.DownloadFile(BaseUrl + path, target);
        }

        public void UploadLicense(string file) { UploadFile("/api/license/activate", file, "license"); }
        public void RestoreBackup(string file) { UploadFile("/api/restore", file, "file"); }

        void UploadFile(string path, string file, string field) {
            string boundary = "----GRM" + DateTime.UtcNow.Ticks.ToString("x");
            var req = (HttpWebRequest)WebRequest.Create(BaseUrl + path);
            req.Method = "POST";
            req.Timeout = 60000;
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            byte[] head = Encoding.UTF8.GetBytes("--"+boundary+"\r\nContent-Disposition: form-data; name=\""+field+"\"; filename=\""+Path.GetFileName(file).Replace("\"","")+"\"\r\nContent-Type: application/octet-stream\r\n\r\n");
            byte[] tail = Encoding.UTF8.GetBytes("\r\n--"+boundary+"--\r\n");
            var fi = new FileInfo(file);
            req.ContentLength = head.Length + fi.Length + tail.Length;
            using (var s=req.GetRequestStream()) {
                s.Write(head,0,head.Length);
                using (var f=File.OpenRead(file)) f.CopyTo(s);
                s.Write(tail,0,tail.Length);
            }
            try {
                using (var resp=(HttpWebResponse)req.GetResponse()) {}
            } catch (WebException ex) {
                string msg=ex.Message;
                try { using(var sr=new StreamReader(ex.Response.GetResponseStream(),Encoding.UTF8)){var d=js.Deserialize<Dictionary<string,object>>(sr.ReadToEnd()); if(d!=null&&d.ContainsKey("error"))msg=Convert.ToString(d["error"]);} } catch{}
                throw new InvalidOperationException(msg, ex);
            }
        }

        public void Dispose() {}
    }

    public static class J {
        public static Dictionary<string,object> D(object o) { return o as Dictionary<string,object> ?? new Dictionary<string,object>(); }
        public static List<Dictionary<string,object>> LD(object o) {
            var r=new List<Dictionary<string,object>>();
            var arr=o as object[];
            if(arr!=null){foreach(var x in arr){var d=D(x); if(d.Count>0)r.Add(d);} return r;}
            var list=o as ArrayList; if(list!=null){foreach(var x in list){var d=D(x); if(d.Count>0)r.Add(d);}}
            return r;
        }
        public static string S(Dictionary<string,object> d,string k,string def=""){object v;return d!=null&&d.TryGetValue(k,out v)&&v!=null?Convert.ToString(v):def;}
        public static bool B(Dictionary<string,object> d,string k,bool def=false){object v;if(d==null||!d.TryGetValue(k,out v)||v==null)return def;bool b;if(bool.TryParse(Convert.ToString(v),out b))return b;int i;if(int.TryParse(Convert.ToString(v),out i))return i!=0;return def;}
        public static int I(Dictionary<string,object> d,string k,int def=0){object v;int n;return d!=null&&d.TryGetValue(k,out v)&&v!=null&&int.TryParse(Convert.ToString(v),out n)?n:def;}
        public static double F(Dictionary<string,object> d,string k,double def=0){object v;double n;return d!=null&&d.TryGetValue(k,out v)&&v!=null&&double.TryParse(Convert.ToString(v),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out n)?n:def;}
        public static Dictionary<string,object> Clone(Dictionary<string,object> d){var r=new Dictionary<string,object>();if(d!=null)foreach(var kv in d)r[kv.Key]=kv.Value;return r;}
        public static bool Perm(Dictionary<string,object> root,string key){Dictionary<string,object> p;object o; if(root!=null&&root.TryGetValue("permissions",out o)){p=D(o);return B(p,key);} return false;}
    }
}
