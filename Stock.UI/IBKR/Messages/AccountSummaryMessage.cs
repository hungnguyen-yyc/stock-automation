/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace Stock.UI.IBKR.Messages
{
    class AccountSummaryMessage
    {
        public int RequestId { get; set; }

        public string Account { get; set; }

        public string Tag { get; set; }

        public string Value { get; set; }

        public string Currency { get; set; }

        public AccountSummaryMessage(int requestId, string account, string tag, string value, string currency)
        {
            RequestId = requestId;
            Account = account;
            Tag = tag;
            Value = value;
            Currency = currency;
        }

        public override string ToString()
        {
            return $"RequestId: {RequestId}\nAccount: {Account}\nTag: {Tag}\nValue: {Value}\nCurrency: {Currency}";
        }
    }
}
