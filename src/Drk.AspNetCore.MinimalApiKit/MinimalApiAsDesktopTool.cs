using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if WINDOWS
using H.NotifyIcon.Core;
#endif

namespace Drk.AspNetCore.MinimalApiKit;
public static class MinimalApiAsDesktopTool
{
    public static void RunAsDesktopTool(this WebApplication app, DesktopToolOptions options = null!)
    {
        options = options ?? new DesktopToolOptions();
        int sseCnnCount = 0;
        bool sseTriggered = false;
        int bps = options.HeartBeatsPerSecond;

        app.Urls.Add("http://127.0.0.1:0");
        app.MapGet("/sse", async (context) =>
        {
            sseTriggered = true;
            var resp = context.Response;
            Interlocked.Increment(ref sseCnnCount);
            resp.Headers.Add("Content-Type", "text/event-stream");
            try
            {
                //set reconnetion timeout
                await resp.WriteAsync($"retry: {1000 / bps}\n\n");
                for (int i = 0; i < 60 * bps; i++)
                {
                    await resp.WriteAsync($"data: {i}\n\n");
                    await resp.Body.FlushAsync();
                    if (context.RequestAborted.IsCancellationRequested)
                        break;
                    await Task.Delay(1000 / bps);
                }
            }
            finally
            {
                Interlocked.Decrement(ref sseCnnCount);
            }
        });

        app.MapGet("/sse.js", () => @"
var evtSrc = new EventSource('/sse');
var disConnCount = 0;
evtSrc.onmessage = function(e) { disConnCount = 0; };
var hdn = setInterval(function() {
    disConnCount++;
    if (disConnCount >= 5) {
        clearInterval(hdn);               
        evtSrc.close();
        document.getElementsByTagName('body')[0].innerText = 'Disconnected';
    }
}, 1000);
        ");

        var task = app.RunAsync();

        if (!Debugger.IsAttached)
        {
            var url = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                    .Addresses.First();
            //ref https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
                { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else
                throw new ApplicationException("Unsupported OS");
        }

        // watch dog
        var appLife = app.Services.GetRequiredService<IHostApplicationLifetime>();
        Task.Factory.StartNew(async () =>
        {
            int zeroCount = 0;
            while (!sseTriggered)
            {
                await Task.Delay(1000);
            }
            while (zeroCount <= 2 * bps)
            {
                if (sseCnnCount != 0) zeroCount = 0;
                else zeroCount++;
                await Task.Delay(1000 / bps);
            }
            appLife.StopApplication();
        });

        task.Wait();
    }

#if WINDOWS
    static bool exitFlag = false;
    public static string AppBaseUrl = null;

    public static void RunWithNotifyIcon(this WebApplication app, NotifyIconOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options requried");

        var task = app.RunAsync();

        // Get web url
        AppBaseUrl = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()
            .Addresses.First();

        using var iconStream = options.IconStream;
        using var icon = new Icon(iconStream);
        using var trayIcon = new TrayIconWithContextMenu
        {
            Icon = icon.Handle,
            ToolTip = options.ToolTip
        };

        trayIcon.ContextMenu = new PopupMenu();
        
        

        foreach (var item in options.MenuItems)
        {
            trayIcon.ContextMenu.Items.Add(item);
        }
        
        if (options.MenuItems.Count > 0)
            trayIcon.ContextMenu.Items.Add(new PopupMenuSeparator());
        trayIcon.ContextMenu.Items.Add(
            new PopupMenuItem(options.ExitMenuItemText, (_, _) =>
            {
                trayIcon.Dispose();
                exitFlag = true;
            }));
        
        trayIcon.Create();
        trayIcon.Show();

        var appLife = app.Services.GetRequiredService<IHostApplicationLifetime>();
        Task.Factory.StartNew(async () =>
        {
            while (!exitFlag)
            {
                await Task.Delay(100);
            }
            appLife.StopApplication();
        });
        
        task.Wait();
    }
#endif

}
