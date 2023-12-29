using IBApi;
using IBKR.Services.messages;
using System.Net;

namespace IBKR.Services
{
    public class IBKRConnectionStateService : IConnectionService
    {
        private IBClient _ibClient;
        private const string ClientId = "1"; // hardcoded for now, may not needed
        private EReaderMonitorSignal _signal;

        public IBKRConnectionStateService()
        {
            _signal = new EReaderMonitorSignal();
            _ibClient = new IBClient(_signal);
            _ibClient.NextValidId += OnNextValidId;
        }

        private void OnNextValidId(ConnectionStatusMessage statusMessage)
        {
            IsConnected = statusMessage.IsConnected;
        }

        public bool IsConnected { get; private set; }

        public void Connect(string host = "127.0.0.1", int port = 7496)
        {
            if (!IsConnected)
            {
                if (host == null || host.Equals(""))
                    host = "127.0.0.1";
                try
                {
                    _ibClient.ClientId = int.Parse(ClientId);
                    _ibClient.ClientSocket.eConnect(host, port, _ibClient.ClientId);

                    var reader = new EReader(_ibClient.ClientSocket, _signal);

                    reader.Start();

                    new Thread(() => { while (_ibClient.ClientSocket.IsConnected()) { _signal.waitForSignal(); reader.processMsgs(); } }) { IsBackground = true }.Start();
                }
                catch (Exception)
                {
                    throw new Exception("Error code -1. Please check your connection attributes.");
                }
            }
            else
            {
                IsConnected = false;
                _ibClient.ClientSocket.eDisconnect();
            }
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }
    }

    public interface IConnectionService
    {
        bool IsConnected { get; }
        void Connect(string host = "127.0.0.1", int port = 7496);
        void Disconnect();
    }
}
