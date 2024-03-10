﻿/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using IBApi;

namespace Stock.Shared.Models.IBKR.Messages
{
    public class PositionMessage
    {
        public PositionMessage(string account, Contract contract, decimal pos, double avgCost)
        {
            Account = account;
            Contract = contract;
            Position = pos;
            AverageCost = avgCost;
        }

        public string Account { get; set; }

        public Contract Contract { get; set; }

        public decimal Position { get; set; }

        public double AverageCost { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            } 
            else
            {
                var position = (PositionMessage)obj;
                return Account == position.Account
                    && Contract.Equals(position.Contract)
                    && Position == position.Position
                    && AverageCost == position.AverageCost;
            }
        }
    }
}