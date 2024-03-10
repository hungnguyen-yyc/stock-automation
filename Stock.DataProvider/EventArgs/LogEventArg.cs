namespace Stock.Data.EventArgs
{
    public class LogEventArg
    {
        public LogEventArg(string message) : this(DateTime.Now, message)
        {
        }

        public LogEventArg(DateTime time, string message)
        {
            Time = time;
            Message = message;
        }

        public string Message { get; }
        public DateTime Time { get; }
    }
}
