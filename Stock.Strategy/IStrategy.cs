namespace Stock.Strategies
{
    public interface IStrategy
    {
        string Description { get; }
        
        event AlertEventHandler EntryAlertCreated;
        
        event AlertEventHandler ExitAlertCreated;
    }
}