using Stock.Shared.Models;

namespace Stock.Strategies
{
    public class AlertEventArgs : EventArgs
    {
        public Alert Alert { get; set; }

        public AlertEventArgs(Alert alert)
        {
            Alert = alert;
        }
    }
}
