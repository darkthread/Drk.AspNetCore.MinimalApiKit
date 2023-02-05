using H.NotifyIcon.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Diagnostics;
using System.Drawing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinApiTrayIcon;
using Drk.AspNetCore.MinimalApiKit;
using System.Runtime.CompilerServices;

const string appToolTip = "AES256 Demo";
const string appUuid = "{9BE6C0F7-13F3-47BA-8B91-FB6A50BE09C5}";

// Prevent re-entrance
using (Mutex m = new Mutex(false, $"Global\\{appUuid}"))
{
    if (!m.WaitOne(0, false))
    {
        return;
    }

    bool exitFlag = false;
    
    var builder = WebApplication.CreateBuilder(args);
    var app = builder.Build();

    app.MapGet("/", () => Results.Content(@"<!DOCTYPE html>
<html><head>
    <meta charset=utf-8>
    <title>AES256 Encryption/Decryption Demo</title>
    <style>
    textarea { width: 300px; display: block; margin-top: 3px; }
    div > * { margin-right: 3px; }
    </style>
</head>
<body>
    <div>
    <input id=key /><button onclick=encrypt()>Encrypt</button><button onclick=decrypt()>Decrypt</button>
    </div>
    <textarea id=plain></textarea>
    <textarea id=enc></textarea>
    <script>
    let setVal = (id,v) => document.getElementById(id).value=v;
    let val = (id) => document.getElementById(id).value;
    let getFetchOpt = (data) => {
        return {
            method: 'POST', headers: { 'Content-Type': 'application/json', 'Accept': 'text/plain' },
            body: JSON.stringify(data)
        }
    };
    function encrypt() { 
        setVal('enc', '');
        fetch('/enc',getFetchOpt({ key: val('key'), plain: val('plain') }))
        .then(r => r.text()).then(t => setVal('enc',t)); }
    function decrypt() { 
        setVal('plain', '');
        fetch('/dec',getFetchOpt({ key: val('key'), enc: val('enc') }))
        .then(r => r.text()).then(t => setVal('plain',t)); }
    </script>
</body></html>", "text/html"));
    Func<Func<string>, string> catchEx = (fn) =>
    {
        try { return fn(); } catch (Exception ex) { return "ERROR:" + ex.Message; }
    };
    app.MapPost("/enc", (DataObject data) => catchEx(() => AesUtil.AesEncrypt(data.key, data.plain)));
    app.MapPost("/dec", (DataObject data) => catchEx(() => AesUtil.AesDecrypt(data.key, data.enc)));

    app.RunWithNotifyIcon(new NotifyIconOptions
    {
        IconStream = typeof(Program).Assembly.GetManifestResourceStream($"RunWithNotifyIconDemo.App.ico"),
        ToolTip = appToolTip, 
        MenuItems =
        {
            NotifyIconOptions.CreateLaunchBrowserMenuItem("Launch Browser", (webBaseUrl) => webBaseUrl),
            NotifyIconOptions.CreateMenuSeparator(),
            NotifyIconOptions.CreateActionMenuItem("Say Hello", (state) =>
            {
                System.Windows.Forms.MessageBox.Show("Hello, World!");
            })
        }
    });
}

class DataObject
{
    public string key { get; set; }
    public string plain { get; set; }
    public string enc { get; set; }
}
