using Newtonsoft.Json;
using Stock.Shared.Models;
using Stock.Shared.Models.Parameters;

namespace Stock.Data
{
    public class StockDataRetrievalService
    {
        // log delegate
        public event LogEventHander LogCreated;

        private void Log(string message)
        {
            Console.WriteLine(message);
            LogCreated?.Invoke(new EventArgs.LogEventArg(message));
        }
        
        public async Task<IReadOnlyCollection<OptionsScreeningResult>> GetOptionsScreeningResults(OptionsScreeningParams requestParams, bool eod)
        {
            try
            {
                using var httpClient = new HttpClient();
                var endPoint = "https://webapp-proxy.aws.barchart.com/v1/ondemand/getOptionsScreener.json";
                var results = new List<OptionsScreeningResult>();
                var urls = new List<string>()
                {
                    $"{endPoint}{requestParams.ToQueryString(OptionsScreeningParams.INSTRUMENT_TYPE_STOCKS, eod)}",
                    $"{endPoint}{requestParams.ToQueryString(OptionsScreeningParams.INSTRUMENT_TYPE_ETF, eod)}"
                };
                
                foreach (var url in urls)
                {
                    var page = 1;
                    while (true)
                    {
                        var pagedUrl = $"{url}&page={page}";
                        var response = await httpClient.GetAsync(pagedUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var optionsScreeningResponse = OptionsScreeningResponse.FromJson(content);
                        
                            if (optionsScreeningResponse.Results == null)
                            {
                                break;
                            }
                        
                            results.AddRange(optionsScreeningResponse.Results);
                            page++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            return new List<OptionsScreeningResult>();
        }

        public async Task<OptionPriceList?> GetOptionPriceAsync(string optionDetailByBarChart)
        {
            try
            {
                using var httpClient = new HttpClient();
                var optionDetail = optionDetailByBarChart.Split('|');
                var symbol = optionDetail[0];
                var expiryDate = optionDetail[1];
                var strikePrice = Decimal.Parse(optionDetail[2].Substring(0, optionDetail[2].Length - 1)).ToString("F2");
                var optionType = optionDetail[2].Substring(optionDetail[2].Length - 1);
                var url = "https://webapp-proxy.aws.barchart.com/v1/ondemand/getEquityOptionsHistory.json?symbol={0}%7C{1}%7C{2}{3}&fields=volatility,theoretical,delta,gamma,theta,vega,rho,openInterest,volume,trades";
                var formattedUrl = string.Format(url, symbol, expiryDate, strikePrice, optionType);
                var response = await httpClient.GetAsync(formattedUrl);
                OptionPriceList options = null;

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var optionPrice = OptionPriceResponse.FromJson(content);

                    if (optionPrice?.OptionPrice == null)
                    {
                        return null;
                    }
                    
                    if (optionPrice.OptionPrice.Length == 0)
                    {
                        return null;
                    }

                    options = new OptionPriceList(optionPrice.OptionPrice);
                }

                return options;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }

        public async Task<Options?> GetOptionChainAsync(string ticker, DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var httpClient = new HttpClient();
                var url = "https://instruments-excel.aws.barchart.com/instruments/{0}/options?expired=true&symbols=true&start={1}&end={2}";
                var formattedUrl = string.Format(url, ticker, fromDate.ToString("yyyy-MM-dd"), toDate.ToString("yyyy-MM-dd"));
                var response = await httpClient.GetAsync(formattedUrl);
                Options? options = null;

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var optionChains = OptionChain.FromJson(content);

                    if (optionChains == null)
                    {
                        return null;
                    }

                    options = optionChains.Options;
                }

                return options;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return null;
            }
        }
        
        public async Task<Price?> GetLatestPrice(string ticker)
        {
            try
            {
                var today = DateTime.Now.ToString("yyyyMMdd");
                var tomorrow = DateTime.Now.AddDays(1).ToString("yyyyMMdd");
                var url = "https://ds01.ddfplus.com/historical/queryminutes.ashx?symbol={0}&start={1}&end={2}&contractroll=combined&order=Descending&interval=1&fromt=false&username=randacchub%40gmail.com&password=_placeholder_";
                var formattedUrl = string.Format(url, ticker, today, tomorrow);
                
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(formattedUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var lines = content.Split('\n');
                    var latest = lines.Last();
                    var price = new Price();
                    var values = latest.Split(',');

                    price.Date = Convert.ToDateTime(values[0]);
                    price.Open = Convert.ToDecimal(values[2]);
                    price.High = Convert.ToDecimal(values[3]);
                    price.Low = Convert.ToDecimal(values[4]);
                    price.Close = Convert.ToDecimal(values[5]);
                    price.Volume = Convert.ToInt64(values[6]);

                    if (!price.isValid)
                    {
                        throw new Exception($"Invalid price {JsonConvert.SerializeObject(price)}");
                    }
                    return price;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error getting data for ticker {ticker}", ex);
            }
            
            return null;
        }
        
        public async Task<IReadOnlyCollection<Price>> GetStockDataForHighTimeframesAsc(string ticker, Timeframe timeframe, DateTime from, DateTime to)
        {
            try
            {
                var fromString = from.ToString("yyyyMMdd");
                var interval = 60;
                var toString = to.ToString("yyyyMMdd");

                switch (timeframe)
                {
                    case Timeframe.Hour1:
                        interval = 60;
                        break;
                    case Timeframe.Daily:
                        break;
                    default:
                        throw new Exception("Timeframe not supported");
                }

                var url = "https://ds01.ddfplus.com/historical/queryminutes.ashx?symbol={0}&start={1}&end={2}&contractroll=combined&order=Descending&interval={3}&fromt=false&username=randacchub%40gmail.com&password=_placeholder_";
                var formatedUrl = string.Format(url, ticker, fromString, toString, interval);
                if (timeframe == Timeframe.Daily)
                {
                    url =
                        "https://ds01.ddfplus.com/historical/queryeod.ashx?symbol={0}&start={1}&end={2}&contractroll=combined&order=Descending&fromt=false&username=randacchub%40gmail.com&password=_placeholder_";
                    formatedUrl = string.Format(url, ticker, fromString, toString);
                }
                
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(formatedUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var lines = content.Split('\n');
                    var list = new List<Price>();
                    foreach (var line in lines)
                    {
                        var price = new Price();
                        var values = line.Split(',');
                        if (values.Length < 7)
                        {
                            continue;
                        }

                        if (timeframe == Timeframe.Daily)
                        {
                            price.Date = Convert.ToDateTime(values[1]);
                            price.Open = Convert.ToDecimal(values[2]);
                            price.High = Convert.ToDecimal(values[3]);
                            price.Low = Convert.ToDecimal(values[4]);
                            price.Close = Convert.ToDecimal(values[5]);
                            price.Volume = Convert.ToInt64(values[6]);
                        }
                        else
                        {
                            price.Date = Convert.ToDateTime(values[0]);
                            price.Open = Convert.ToDecimal(values[2]);
                            price.High = Convert.ToDecimal(values[3]);
                            price.Low = Convert.ToDecimal(values[4]);
                            price.Close = Convert.ToDecimal(values[5]);
                            price.Volume = Convert.ToInt64(values[6]);
                        }

                        if (!price.isValid)
                        {
                            throw new Exception($"Invalid price {JsonConvert.SerializeObject(price)}");
                        }

                        list.Add(price);
                    }
                    return list.OrderBy(x => x.Date).ToList();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                throw new Exception($"Error getting data for ticker {ticker}", ex);
            }
            
            return new List<Price>();
        }

        private DateTime GetEasternTime(DateTime time)
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var easternTime = TimeZoneInfo.ConvertTime(time, easternZone);
            return easternTime;
        }
    }
}
