using EngineShell.Application.Interfaces;

namespace EngineShell.Application.Services
{
    public class AppStatusService : IAppStatusService
    {
        private string _status = "Ready";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                
            }
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;

            }
        }

        public void Log(string message)
        {
            // optional: write to file, console, or in-memory list
        }
    }



}
