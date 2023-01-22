using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockSignalScanner.Repositories
{
    internal abstract class BaseRepository
    {
        protected readonly string _connectionString = @"Data Source=C:\Users\hnguyen\Documents\stock-scan-logs\stock-scanner-analytics.db;";
        protected BaseRepository() {}
    }
}
