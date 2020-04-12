using DirectX12GameEngine.Editor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Editor.Views
{
    public class MainPageNavigationParameters
    {
        public MainPageNavigationParameters(MainViewModel viewModel, string arguments)
        {
            ViewModel = viewModel;
            Arguments = arguments;
        }

        public MainViewModel ViewModel { get; }

        public string Arguments { get; }
    }
}
