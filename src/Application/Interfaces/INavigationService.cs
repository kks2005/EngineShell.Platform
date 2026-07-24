using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EngineShell.Application.Interfaces
{
    public interface INavigationService
    {
        object CurrentViewModel { get; }
        void NavigateTo(string key);
    }
}
