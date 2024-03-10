﻿/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

namespace Stock.Shared.Models.IBKR.Messages
{
    public abstract class OrderMessage
    {
        protected int orderId;

        public int OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }
    }
}
