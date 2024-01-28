using Stock.Shared.Models;

namespace Stock.Strategies.EventArgs
{
    public class AlertEventArgs : System.EventArgs
    {
        public Alert Alert { get; set; }

        public AlertEventArgs(Alert alert)
        {
            Alert = alert;
        }
    }
}
