using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H.NotifyIcon.Core;

namespace Drk.AspNetCore.MinimalApiKit
{
    public class NotifyIconOptions
    {
        public Stream IconStream { get; set; }
        public string ToolTip { get; set; }
        public string ExitMenuItemText { get; set; } = "Exit";
        public ICollection<PopupItem> MenuItems { get; } = new List<PopupItem>();
        public static PopupMenuItem CreateActionMenuItem(string menuItemText, Action<NotifyIconMenuStateObject> menuItemAction)
        {
            return new PopupMenuItem(menuItemText, (_, _) =>
            {
                menuItemAction(new NotifyIconMenuStateObject(MinimalApiAsDesktopTool.AppBaseUrl, menuItemText));
            });
        }
        public static PopupMenuItem CreateLaunchBrowserMenuItem(string menuItemText, Func<string, string> getFullUrl)
            => CreateActionMenuItem(menuItemText, (state) =>
            {
                var url = getFullUrl(state.AppBaseUrl);
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
                    { CreateNoWindow = true });
            });

        public static PopupItem CreateMenuSeparator() => new PopupMenuSeparator();

    }
}
