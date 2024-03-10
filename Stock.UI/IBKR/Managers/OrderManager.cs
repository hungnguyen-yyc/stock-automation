using IBApi;
using Stock.UI.IBKR.Client;
using Stock.Shared.Models.IBKR.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stock.UI.IBKR.Managers
{
    internal class OrderManager
    {
        private List<string> managedAccounts;

        private List<OpenOrderMessage> openOrders = new List<OpenOrderMessage>();
        private List<CompletedOrderMessage> completedOrders = new List<CompletedOrderMessage>();

        public IBClient IBClient { get; }

        public delegate void OpenOrderHandler(List<OpenOrderMessage> openOrder);
        public event OpenOrderHandler OpenOrderReceived;

        public delegate void CompletedOrderHandler(List<CompletedOrderMessage> completedOrder);
        public event CompletedOrderHandler CompletedOrderReceived;

        public OrderManager(IBClient ibClient)
        {
            IBClient = ibClient;
            IBClient.OpenOrder += handleOpenOrder;
            IBClient.CompletedOrder += handleCompletedOrder;
        }

        public List<string> ManagedAccounts
        {
            get { return managedAccounts; }
            set
            {
                managedAccounts = value;
            }
        }

        public void PlaceOrder(Contract contract, Order order)
        {
            if (order.OrderId != 0)
            {
                IBClient.ClientSocket.placeOrder(order.OrderId, contract, order);
            }
            else
            {
                IBClient.ClientSocket.placeOrder(IBClient.NextOrderId, contract, order);
                IBClient.NextOrderId++;
            }
        }

        public void CancelOrder(Order order, string manualOrderCancelTime)
        {
            if (order.OrderId != 0)
            {
                IBClient.ClientSocket.cancelOrder(order.OrderId, manualOrderCancelTime);
            }
        }

        public void handleOpenOrder(OpenOrderMessage openOrder)
        {
            for (int i = 0; i < openOrders.Count; i++)
            {
                if (openOrders[i].Order.OrderId == openOrder.OrderId)
                {
                    openOrders[i] = openOrder;
                    return;
                }
            }
            openOrders.Add(openOrder);
            OpenOrderReceived?.Invoke(openOrders);
        }

        public void handleCompletedOrder(CompletedOrderMessage completedOrder)
        {
            var hasOrder = completedOrders.Any(x => 
                x.OrderState.CompletedStatus == completedOrder.OrderState.CompletedStatus
                && x.OrderState.CompletedTime == completedOrder.OrderState.CompletedTime
                && x.Contract.LocalSymbol == completedOrder.Contract.LocalSymbol);
            if (hasOrder)
            {
                return;
            }
            completedOrders.Add(completedOrder);

            CompletedOrderReceived?.Invoke(completedOrders);
        }
    }
}
