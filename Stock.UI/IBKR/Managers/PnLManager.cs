using Stock.Shared.Models.IBKR.Messages;
using Stock.UI.IBKR.Client;
using Stock.UI.IBKR.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.UI.IBKR.Managers
{
    internal class PnLManager
    {
        private int pnlReqId;
        private IBClient ibClient;
        private int pnlSingleReqId;

        public delegate void PnLHandler(int reqId, double dailyPnL, double unrealizedPnL, double realizedPnL);
        public event PnLHandler PnLReceived;

        public PnLManager(IBClient ibClient)
        {
            this.ibClient = ibClient;
            ibClient.pnl += HandlePnL;
        }

        public void ReqPnL(string account, string modelCode)
        {
            pnlReqId = new Random(DateTime.Now.Millisecond).Next();

            ibClient.ClientSocket.reqPnL(pnlReqId, account, modelCode);
        }

        public void CancelPnL()
        {
            if (pnlReqId != 0)
            {
                ibClient.ClientSocket.cancelPnL(pnlReqId);

                pnlReqId = 0;
            }
        }

        public void ReqPnLSingle(string account, string modelCode, int conId)
        {
            pnlSingleReqId = new Random(DateTime.Now.Millisecond).Next();

            ibClient.ClientSocket.reqPnLSingle(pnlSingleReqId, account, modelCode, conId);
        }

        public void CancelPnLSingle()
        {
            if (pnlSingleReqId != 0)
            {
                ibClient.ClientSocket.cancelPnLSingle(pnlSingleReqId);

                pnlSingleReqId = 0;
            }
        }

        private void HandlePnL(PnLMessage message)
        {
            PnLReceived?.Invoke(message.ReqId, message.DailyPnL, message.UnrealizedPnL, message.RealizedPnL);
        }
    }
}
