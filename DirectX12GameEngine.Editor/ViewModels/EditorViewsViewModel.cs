using DirectX12GameEngine.Editor.Messages;
using DirectX12GameEngine.Mvvm;
using DirectX12GameEngine.Mvvm.Messaging;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EditorViewsViewModel : ViewModelBase
    {
        private bool isSolutionExplorerOpen;
        private bool isPropertyGridOpen;

        public EditorViewsViewModel()
        {
            Messenger.Default.Register<ProjectLoadedMessage>(this, m => { IsSolutionExplorerOpen = true; IsPropertyGridOpen = true; });
        }

        public bool IsSolutionExplorerOpen
        {
            get => isSolutionExplorerOpen;
            set
            {
                if (Set(ref isSolutionExplorerOpen, value))
                {
                    if (isSolutionExplorerOpen)
                    {
                        Messenger.Default.Send(new OpenViewMessage("SolutionExplorer"));
                    }
                    else
                    {
                        Messenger.Default.Send(new CloseViewMessage("SolutionExplorer"));
                    }
                }
            }
        }

        public bool IsPropertyGridOpen
        {
            get => isPropertyGridOpen;
            set
            {
                if (Set(ref isPropertyGridOpen, value))
                {
                    if (isPropertyGridOpen)
                    {
                        Messenger.Default.Send(new OpenViewMessage("PropertyGrid"));
                    }
                    else
                    {
                        Messenger.Default.Send(new CloseViewMessage("PropertyGrid"));
                    }
                }
            }
        }
    }
}
