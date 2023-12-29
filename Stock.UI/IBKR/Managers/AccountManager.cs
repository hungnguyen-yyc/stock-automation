using System.Collections.Generic;
using Stock.UI.IBKR.Client;
using Stock.UI.IBKR.Messages;
using Stock.UI.IBKR.Utilities;
using static IBApi.Util;

namespace Stock.UI.IBKR.Managers
{
    class AccountManager
    {
        private const int ACCOUNT_ID_BASE = 50000000;

        private const int ACCOUNT_SUMMARY_ID = ACCOUNT_ID_BASE + 1;

        private const string ACCOUNT_SUMMARY_TAGS = "AccountType,NetLiquidation,TotalCashValue,SettledCash,AccruedCash,BuyingPower,EquityWithLoanValue,PreviousEquityWithLoanValue,"
             + "GrossPositionValue,ReqTEquity,ReqTMargin,SMA,InitMarginReq,MaintMarginReq,AvailableFunds,ExcessLiquidity,Cushion,FullInitMarginReq,FullMaintMarginReq,FullAvailableFunds,"
             + "FullExcessLiquidity,LookAheadNextChange,LookAheadInitMarginReq ,LookAheadMaintMarginReq,LookAheadAvailableFunds,LookAheadExcessLiquidity,HighestSeverity,DayTradesRemaining,Leverage";

        private List<string> managedAccounts;

        private bool accountSummaryRequestActive;
        private bool accountUpdateRequestActive;
        private string currentAccountSubscribedToTupdate;

        public delegate void AccountSummaryHandler(AccountSummaryMessage accountSummary);
        public event AccountSummaryHandler AccountSummaryReceived;

        public delegate void AccountSummaryEndHandler(AccountSummaryEndMessage accountSummaryEndMessage);
        public event AccountSummaryEndHandler AccountSummaryEndReceived;

        public delegate void PositionHandler(PositionMessage positionMessage);
        public event PositionHandler PositionReceived;

        public AccountManager(IBClient ibClient)
        {
            IbClient = ibClient;
            IbClient.AccountSummary += HandleAccountSummary;
            IbClient.AccountSummaryEnd += HandleAccountSummaryEnd;
            IbClient.Position += HandlePosition;
        }

        private void HandleAccountSummaryEnd(AccountSummaryEndMessage accountSummary)
        {
            accountSummaryRequestActive = false;
            AccountSummaryEndReceived?.Invoke(accountSummary);
        }

        public void HandleAccountSummary(AccountSummaryMessage summaryMessage)
        {
            AccountSummaryReceived?.Invoke(summaryMessage);
        }

        public void HandleAccountValue(AccountValueMessage accountValueMessage)
        {
            var something = accountValueMessage;
        }

        public void HandlePortfolioValue(UpdatePortfolioMessage updatePortfolioMessage)
        {
            var something = updatePortfolioMessage;
        }

        public void HandlePosition(PositionMessage positionMessage)
        {
            PositionReceived?.Invoke(positionMessage);
        }

        public void HandleFamilyCodes(FamilyCodesMessage familyCodesMessage)
        {
            var something = familyCodesMessage;
        }

        public void RequestAccountSummary()
        {
            if (!accountSummaryRequestActive)
            {
                accountSummaryRequestActive = true;
                IbClient.ClientSocket.reqAccountSummary(ACCOUNT_SUMMARY_ID, "All", ACCOUNT_SUMMARY_TAGS);
            }
            else
            {
                IbClient.ClientSocket.cancelAccountSummary(ACCOUNT_SUMMARY_ID);
            }
        }

        public void SubscribeAccountUpdates()
        {
            if (!accountUpdateRequestActive)
            {
                accountUpdateRequestActive = true;
                IbClient.ClientSocket.reqAccountUpdates(true, currentAccountSubscribedToTupdate);
            }
            else
            {
                IbClient.ClientSocket.reqAccountUpdates(false, currentAccountSubscribedToTupdate);
                currentAccountSubscribedToTupdate = null;
                accountUpdateRequestActive = false;
            }
        }

        public void RequestPositions()
        {
            IbClient.ClientSocket.reqPositions();
        }

        public void RequestFamilyCodes()
        {
            IbClient.ClientSocket.reqFamilyCodes();
        }

        public void ClearFamilyCodes()
        {
        }

        public List<string> ManagedAccounts
        {
            get { return managedAccounts; }
            set
            {
                managedAccounts = value;
                SetManagedAccounts(value);
            }
        }

        public void SetManagedAccounts(List<string> managedAccounts)
        {
        }

        public IBClient IbClient { get; set; }
    }
}
