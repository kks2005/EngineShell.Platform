using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineShell.Application.Models
{
    public class NavigationItem
    {
        public string Title { get; }
        public string Icon { get; }
        public object ViewModel { get; }

        public NavigationItem(string title, string icon, object vm)
        {
            Title = title;
            Icon = icon;
            ViewModel = vm;
        }
    }

}
