// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.UI;
using Newtonsoft.Json.Linq;

namespace LibreHardwareMonitor.Utilities;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly Node _root;
    private Thread _listenerThread;
    private readonly string _defaultDocumentName = "index.html";

    /// <summary>
    /// Mime Type conversion table
    /// </summary>
    private static IDictionary<string, string> _mimeTypeMappings =
        new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
                #region extension to MIME type list
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"},

            #endregion
        };

    public HttpServer(Node node, int port, bool authEnabled = false, string userName = "", string password = "")
    {
        _root = node;
        ListenerPort = port;
        AuthEnabled = authEnabled;
        UserName = userName;
        Password = password;
        try
        {
            _listener = new HttpListener { IgnoreWriteExceptions = true };
        }
        catch (PlatformNotSupportedException)
        {
            _listener = null;
        }
    }

    ~HttpServer()
    {
        if (PlatformNotSupported)
            return;


        StopHttpListener();
        _listener.Abort();
    }

    public bool AuthEnabled { get; set; }

    public int ListenerPort { get; set; }

    public string Password
    {
        get { return PasswordSHA256; }
        set { PasswordSHA256 = ComputeSHA256(value); }
    }

    public bool PlatformNotSupported
    {
        get { return _listener == null; }
    }

    public string UserName { get; set; }

    private string PasswordSHA256 { get; set; }

    public bool StartHttpListener()
    {
        if (PlatformNotSupported)
            return false;


        try
        {
            if (_listener.IsListening)
                return true;


            string prefix = "http://+:" + ListenerPort + "/";
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(prefix);
            _listener.Realm = "Libre Hardware Monitor";
            _listener.AuthenticationSchemes = AuthEnabled ? AuthenticationSchemes.Basic : AuthenticationSchemes.Anonymous;
            _listener.Start();

            if (_listenerThread == null)
            {
                _listenerThread = new Thread(HandleRequests);
                _listenerThread.Start();
            }
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public bool StopHttpListener()
    {
        if (PlatformNotSupported)
            return false;


        try
        {
            _listenerThread?.Abort();
            _listener.Stop();
            _listenerThread = null;
        }
        catch (HttpListenerException)
        { }
        catch (ThreadAbortException)
        { }
        catch (NullReferenceException)
        { }
        catch (Exception)
        { }

        return true;
    }

    private void HandleRequests()
    {
        while (_listener.IsListening)
        {
            IAsyncResult context = _listener.BeginGetContext(ListenerCallback, _listener);
            context.AsyncWaitHandle.WaitOne();
        }
    }

    public static IDictionary<string, string> ToDictionary(NameValueCollection col)
    {
        IDictionary<string, string> dict = new Dictionary<string, string>();
        foreach (string k in col.AllKeys)
        {
            dict.Add(k, col[k]);
        }

        return dict;
    }

    public SensorNode FindSensor(Node node, string id)
    {
        if (node is SensorNode sNode)
        {
            if (sNode.Sensor.Identifier.ToString() == id)
                return sNode;
        }

        foreach (Node child in node.Nodes)
        {
            SensorNode s = FindSensor(child, id);
            if (s != null)
            {
                return s;
            }
        }

        return null;
    }

    public void SetSensorControlValue(SensorNode sNode, string value)
    {
        if (sNode.Sensor.Control == null)
        {
            throw new ArgumentException("Specified sensor '" + sNode.Sensor.Identifier + "' can not be set");
        }

        if (value == "null")
        {
            sNode.Sensor.Control.SetDefault();
        }
        else
        {
            sNode.Sensor.Control.SetSoftware(float.Parse(value, CultureInfo.InvariantCulture));
        }
    }

    //Handles "/Sensor" requests.
    //Parameters are taken from the query part of the URL.
    //Get:
    //http://localhost:8085/Sensor?action=Get&id=/some/node/path/0
    //The output is either:
    //{"result":"fail","message":"Some error message"}
    //or:
    //{"result":"ok","value":42.0, "format":"{0:F2} RPM"}
    //
    //Set:
    //http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=42.0
    //http://localhost:8085/Sensor?action=Set&id=/some/node/path/0&value=null
    //The output is either:
    //{"result":"fail","message":"Some error message"}
    //or:
    //{"result":"ok"}
    private void HandleSensorRequest(HttpListenerRequest request, JObject result)
    {
        IDictionary<string, string> dict = ToDictionary(HttpUtility.ParseQueryString(request.Url.Query));

        if (dict.ContainsKey("action"))
        {
            if (dict.ContainsKey("id"))
            {
                SensorNode sNode = FindSensor(_root, dict["id"]);

                if (sNode == null)
                {
                    throw new ArgumentException("Unknown id " + dict["id"] + " specified");
                }

                switch (dict["action"])
                {
                    case "Set" when dict.ContainsKey("value"):
                        SetSensorControlValue(sNode, dict["value"]);
                        break;
                    case "Set":
                        throw new ArgumentNullException("No value provided");
                    case "Get":
                        result["value"] = sNode.Sensor.Value;
                        result["format"] = sNode.Format;
                        break;
                    default:
                        throw new ArgumentException("Unknown action type " + dict["action"]);
                }
            }
            else
            {
                throw new ArgumentNullException("No id provided");
            }
        }
        else
        {
            throw new ArgumentNullException("No action provided");
        }
    }

    //Handles http POST requests in a REST like manner.
    //Currently the only supported base URL is http://localhost:8085/Sensor.
    private string HandlePostRequest(HttpListenerRequest request)
    {
        JObject result = new JObject { ["result"] = "ok" };

        try
        {
            if (request.Url.Segments.Length == 2)
            {
                if (request.Url.Segments[1] == "Sensor")
                {
                    HandleSensorRequest(request, result);
                }
                else
                {
                    throw new ArgumentException("Invalid URL ('" + request.Url.Segments[1] + "'), possible values: ['Sensor']");
                }
            }
            else
                throw new ArgumentException("Empty URL, possible values: ['Sensor']");
        }
        catch (Exception e)
        {
            result["result"] = "fail";
            result["message"] = e.ToString();
        }
#if DEBUG
        return result.ToString(Newtonsoft.Json.Formatting.Indented);
#else
            return result.ToString(Newtonsoft.Json.Formatting.None);
#endif
    }

    private void Process(HttpListenerContext context)
    {
        string filename = context.Request.Url.ToString();
        string templateName = "";
        string filePath = "";
        var parts = filename.Split('/');
        if (string.Equals(parts[parts.Length - 2], "template", StringComparison.OrdinalIgnoreCase))
        {
            templateName = parts[parts.Length - 1];
            if (string.IsNullOrEmpty(templateName))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.OutputStream.Close();
                return;
            }
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", templateName, _defaultDocumentName);
        }
        else if (string.Equals(parts[parts.Length - 3], "template", StringComparison.OrdinalIgnoreCase))
        {
            templateName = parts[parts.Length - 2];
            if (string.IsNullOrEmpty(templateName))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.OutputStream.Close();
                return;
            }
            filePath = Path.Combine(Directory.GetCurrentDirectory(), "templates", templateName, parts[parts.Length - 1]);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.OutputStream.Close();
            return;
        }
        if (File.Exists(filePath))
        {
            try
            {
                Stream input = new FileStream(filePath, FileMode.Open);

                //Adding permanent http response headers
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filePath), out mime)
                    ? mime
                    : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filePath).ToString("r"));

                byte[] buffer = new byte[1024 * 32];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();
                context.Response.OutputStream.Flush();

                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        context.Response.OutputStream.Close();
    }

    private void ListenerCallback(IAsyncResult result)
    {
        HttpListener listener = (HttpListener)result.AsyncState;
        if (listener == null || !listener.IsListening)
            return;


        // Call EndGetContext to complete the asynchronous operation.
        HttpListenerContext context;
        try
        {
            context = listener.EndGetContext(result);
        }
        catch (Exception)
        {
            return;
        }

        HttpListenerRequest request = context.Request;

        bool authenticated;

        if (AuthEnabled)
        {
            try
            {
                HttpListenerBasicIdentity identity = (HttpListenerBasicIdentity)context.User.Identity;
                authenticated = (identity.Name == UserName) & (ComputeSHA256(identity.Password) == Password);
            }
            catch
            {
                authenticated = false;
            }
        }
        else
        {
            authenticated = true;
        }

        if (authenticated)
        {
            string filename = context.Request.Url.ToString();
            if (filename.Contains("/template/"))
            {
                Process(context);
            }                
            else
            { 

                switch (request.HttpMethod)
                {
                    case "POST":
                        {
                            string postResult = HandlePostRequest(request);

                            Stream output = context.Response.OutputStream;
                            byte[] utfBytes = Encoding.UTF8.GetBytes(postResult);

                            context.Response.AddHeader("Cache-Control", "no-cache");
                            context.Response.ContentLength64 = utfBytes.Length;
                            context.Response.ContentType = "application/json";

                            output.Write(utfBytes, 0, utfBytes.Length);
                            output.Close();

                            break;
                        }
                    case "GET":
                        {
                            string requestedFile = request.RawUrl.Substring(1);

                            if (requestedFile == "data.json")
                            {
                                SendJson(context.Response, request);
                                return;
                            }

                            if (requestedFile.Contains("images_icon"))
                            {
                                ServeResourceImage(context.Response,
                                                   requestedFile.Replace("images_icon/", string.Empty));

                                return;
                            }

                            // default file to be served
                            if (string.IsNullOrEmpty(requestedFile))
                                requestedFile = "index.html";

                            string[] splits = requestedFile.Split('.');
                            string ext = splits[splits.Length - 1];
                            ServeResourceFile(context.Response, "Web." + requestedFile.Replace('/', '.'), ext);

                            break;
                        }
                    default:
                        {
                            context.Response.StatusCode = 404;
                            break;
                        }
                }
            }
        }
        else
        {
            context.Response.StatusCode = 401;
        }

        if (context.Response.StatusCode == 401)
        {
            const string responseString = @"<HTML><HEAD><TITLE>401 Unauthorized</TITLE></HEAD>
  <BODY><H4>401 Unauthorized</H4>
  Authorization required.</BODY></HTML> ";

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = 401;
            Stream output = context.Response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }

        try
        {
            context.Response.Close();
        }
        catch
        {
            // client closed connection before the content was sent
        }
    }

    private void ServeResourceFile(HttpListenerResponse response, string name, string ext)
    {
        // resource names do not support the hyphen
        name = "LibreHardwareMonitor.Resources." +
               name.Replace("custom-theme", "custom_theme");

        string[] names =
            Assembly.GetExecutingAssembly().GetManifestResourceNames();

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Replace('\\', '.') == name)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[i]))
                {
                    response.ContentType = GetContentType("." + ext);
                    response.ContentLength64 = stream.Length;
                    byte[] buffer = new byte[512 * 1024];
                    try
                    {
                        Stream output = response.OutputStream;
                        int len;
                        while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            output.Write(buffer, 0, len);
                        }

                        output.Flush();
                        output.Close();
                        response.Close();
                    }
                    catch (HttpListenerException)
                    { }
                    catch (InvalidOperationException)
                    { }

                    return;
                }
            }
        }

        response.StatusCode = 404;
        response.Close();
    }

    private void ServeResourceImage(HttpListenerResponse response, string name)
    {
        name = "LibreHardwareMonitor.Resources." + name;

        string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i].Replace('\\', '.') == name)
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[i]))
                {
                    Image image = Image.FromStream(stream);
                    response.ContentType = "image/png";
                    try
                    {
                        Stream output = response.OutputStream;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            image.Save(ms, ImageFormat.Png);
                            ms.WriteTo(output);
                        }

                        output.Close();
                    }
                    catch (HttpListenerException)
                    { }

                    image.Dispose();
                    response.Close();
                    return;
                }
            }
        }

        response.StatusCode = 404;
        response.Close();
    }

    private void SendJson(HttpListenerResponse response, HttpListenerRequest request = null)
    {
        JObject json = new JObject();

        int nodeIndex = 0;

        json["id"] = nodeIndex++;
        json["Text"] = "Sensor";
        json["Min"] = "Min";
        json["Value"] = "Value";
        json["Max"] = "Max";
        json["ImageURL"] = string.Empty;

        JArray children = new JArray { GenerateJsonForNode(_root, ref nodeIndex) };
        json["Children"] = children;
#if DEBUG
        string responseContent = json.ToString(Newtonsoft.Json.Formatting.Indented);
#else
            string responseContent = json.ToString(Newtonsoft.Json.Formatting.None);
#endif
        byte[] buffer = Encoding.UTF8.GetBytes(responseContent);

        bool acceptGzip;
        try
        {
            acceptGzip = (request != null) && (request.Headers["Accept-Encoding"].ToLower().IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch
        {
            acceptGzip = false;
        }

        if (acceptGzip)
            response.AddHeader("Content-Encoding", "gzip");

        response.AddHeader("Cache-Control", "no-cache");
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.ContentType = "application/json";
        try
        {
            if (acceptGzip)
            {
                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(buffer, 0, buffer.Length);

                    buffer = ms.ToArray();
                }
            }

            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
        catch (HttpListenerException)
        { }

        response.Close();
    }

    private JObject GenerateJsonForNode(Node n, ref int nodeIndex)
    {
        JObject jsonNode = new JObject
        {
            ["id"] = nodeIndex++,
            ["Text"] = n.Text,
            ["Min"] = string.Empty,
            ["Value"] = string.Empty,
            ["Max"] = string.Empty
        };

        if (n is SensorNode sensorNode)
        {
            jsonNode["SensorId"] = sensorNode.Sensor.Identifier.ToString();
            jsonNode["Type"] = sensorNode.Sensor.SensorType.ToString();
            jsonNode["Min"] = sensorNode.Min;
            jsonNode["Value"] = sensorNode.Value;
            jsonNode["Max"] = sensorNode.Max;
            jsonNode["ImageURL"] = "images/transparent.png";
        }
        else if (n is HardwareNode hardwareNode)
        {
            jsonNode["ImageURL"] = "images_icon/" + GetHardwareImageFile(hardwareNode);
        }
        else if (n is TypeNode typeNode)
        {
            jsonNode["ImageURL"] = "images_icon/" + GetTypeImageFile(typeNode);
        }
        else
        {
            jsonNode["ImageURL"] = "images_icon/computer.png";
        }

        JArray children = new JArray();
        foreach (Node child in n.Nodes)
        {
            children.Add(GenerateJsonForNode(child, ref nodeIndex));
        }

        jsonNode["Children"] = children;

        return jsonNode;
    }

    private static string GetContentType(string extension)
    {
        switch (extension)
        {
            case ".avi": return "video/x-msvideo";
            case ".css": return "text/css";
            case ".doc": return "application/msword";
            case ".gif": return "image/gif";
            case ".htm":
            case ".html": return "text/html";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".js": return "application/x-javascript";
            case ".mp3": return "audio/mpeg";
            case ".png": return "image/png";
            case ".pdf": return "application/pdf";
            case ".ppt": return "application/vnd.ms-powerpoint";
            case ".zip": return "application/zip";
            case ".txt": return "text/plain";
            default: return "application/octet-stream";
        }
    }

    private static string GetHardwareImageFile(HardwareNode hn)
    {
        switch (hn.Hardware.HardwareType)
        {
            case HardwareType.Cpu:
                return "cpu.png";
            case HardwareType.GpuNvidia:
                return "nvidia.png";
            case HardwareType.GpuAmd:
                return "ati.png";
            case HardwareType.GpuIntel:
                return "intel.png";
            case HardwareType.Storage:
                return "hdd.png";
            case HardwareType.Motherboard:
                return "mainboard.png";
            case HardwareType.SuperIO:
                return "chip.png";
            case HardwareType.Memory:
                return "ram.png";
            case HardwareType.Cooler:
                return "fan.png";
            case HardwareType.Network:
                return "nic.png";
            case HardwareType.Psu:
                return "power-supply.png";
            case HardwareType.Battery:
                return "battery.png";
            default:
                return "cpu.png";
        }
    }

    private static string GetTypeImageFile(TypeNode tn)
    {
        switch (tn.SensorType)
        {
            case SensorType.Voltage:
            case SensorType.Current:
                return "voltage.png";
            case SensorType.Clock:
                return "clock.png";
            case SensorType.Load:
                return "load.png";
            case SensorType.Temperature:
                return "temperature.png";
            case SensorType.Fan:
                return "fan.png";
            case SensorType.Flow:
                return "flow.png";
            case SensorType.Control:
                return "control.png";
            case SensorType.Level:
                return "level.png";
            case SensorType.Power:
                return "power.png";
            case SensorType.Noise:
                return "loudspeaker.png";
            case SensorType.Throughput:
                return "throughput.png";
            default:
                return "power.png";
        }
    }

    private string ComputeSHA256(string text)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return string.Concat(hash
                                .ComputeHash(Encoding.UTF8.GetBytes(text))
                                .Select(item => item.ToString("x2")));
        }
    }

    public void Quit()
    {
        if (PlatformNotSupported)
            return;


        StopHttpListener();
        _listener.Abort();
    }
}
