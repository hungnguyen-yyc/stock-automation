﻿using StockSignalScanner.Indicators;

namespace StockSignalScanner.Models
{
    internal class IndicatorParameterPackage
    {
        public int Rsi { get; set; } = 14;
        public int StcShort { get; set; } = 23;
        public int StcLong { get; set; } = 50;
        public int StcCycleLength { get; set; } = 10;
        public double StcFactor { get; set; } = 0.5;
        public int AroonOscillator { get; set; } = 14;
        public int Mfi { get; set; } = 14;
        public int MovingAverage { get; set; } = 20;
        public int PvoFast { get; set; } = 5;
        public int PvoSlow { get; set; } = 20;
        public int PvoSignal { get; set; } = 9;
        public int MacdSlow { get; set; } = 8;
        public int MacdFast { get; set; } = 20;
        public int MacdSignal { get; set; } = 5;
        public int EmaShort { get; set; } = 5;
        public int EmaLong { get; set; } = 20;
        public int KeltnerEmaPeriod { get; set; } = 20;
        public int KeltnerMultiplier { get; set; } = 2;
        public int Atr { get; set; } = 10;
        public int KaufmanMAAdaptive { get; set; } = 10;
        public int Adx { get; set; } = 10;
    }
}
