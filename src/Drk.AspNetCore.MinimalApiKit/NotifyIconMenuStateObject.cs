using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drk.AspNetCore.MinimalApiKit
{
    public class NotifyIconMenuStateObject
    {
        public string AppBaseUrl { get; set; }
        public string MenuItemText { get; set; }

        internal NotifyIconMenuStateObject(string appBaseUrl, string menuItemText)
        {
            AppBaseUrl = appBaseUrl;
            MenuItemText = menuItemText;
        }
    }
}
