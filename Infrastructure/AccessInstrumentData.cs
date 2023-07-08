using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class AccessInstrumentData
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public class InstrumentData
        {
            public int InstrumentId { get; set; }
            public decimal Bid { get; set; }
            public decimal Ask { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Last { get; set; }
            public decimal Low { get; set; }
            public long Volume { get; set; }
            public decimal Mid { get; set; }
            public DateTime Date { get; set; }
            public decimal PrevClose { get; set; }
            public decimal Change { get; set; }
            public decimal OpenChange { get; set; }
            public DateTime LastRowChange { get; set; }
            public decimal ChangePercentage { get; set; }
            public decimal OpenChangePercentage { get; set; }
            public string CurrencyCode { get; set; }
            public string Name { get; set; }
            public decimal YTD { get; set; }
            public decimal Week { get; set; }
            public decimal _2Week { get; set; }
            public decimal Month { get; set; }
            public decimal NoShares { get; set; }
            public decimal MarketCap { get; set; }
            public decimal _52wHigh { get; set; }
            public decimal _52wLow { get; set; }
            public decimal _52wChange { get; set; }
            public decimal _3MonthHigh { get; set; }
            public decimal _3MonthLow { get; set; }
            public decimal _3MonthChange { get; set; }
            public decimal _5YearsChange { get; set; }
            public decimal EPS { get; set; }
            public double SPS { get; set; }
            public decimal DPS { get; set; }
            public double PayoutRatio { get; set; }
            public decimal Turnover { get; set; }
            public decimal NetIncome { get; set; }
            public double TurnoverGrowth { get; set; }
            public double NetIncomeGrowth { get; set; }
            public double BookValueOfShare { get; set; }
            public decimal AllTimeHigh { get; set; }
            public decimal AllTimeLow { get; set; }
            public decimal TotalMarketCap { get; set; }
            public decimal PrevMid { get; set; }
            public decimal _52Highest { get; set; }
            public decimal _52Lowest { get; set; }
            public decimal HighYTD { get; set; }
            public decimal LowYTD { get; set; }
            public string Ticker { get; set; }
            public string MarketName { get; set; }
            public bool BusinessDaysStoT { get; set; }
        }

        public AccessInstrumentData(IConfiguration configuration, ILogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public InstrumentData GetPrice(int instrumentID)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectInstrumentDataById", sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentID);
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        sqlConnection.Open();
                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new InstrumentData
                                {
                                    InstrumentId = (int)reader["InstrumentId"],
                                    Bid = reader.GetFieldValue<decimal>("Bid"),
                                    Ask = reader.GetFieldValue<decimal>("Ask"),
                                    Open = reader.GetFieldValue<decimal>("Open"),
                                    Last = reader.GetFieldValue<decimal>("Last"),
                                    High = reader.GetFieldValue<decimal>("High"),
                                    Low = reader.GetFieldValue<decimal>("Low"),
                                    Volume = reader.GetFieldValue<long>("Volume"),
                                    Mid = reader.GetFieldValue<decimal>("Mid"),
                                    Date = reader.GetFieldValue<DateTime>("Date"),
                                    PrevClose = reader.GetFieldValue<decimal>("PrevClose"),
                                    Change = reader.GetFieldValue<decimal>("Change"),
                                    OpenChange = reader.GetFieldValue<decimal>("OpenChange"),
                                    LastRowChange = reader.GetFieldValue<DateTime>("LastRowChange"),
                                    ChangePercentage = reader.GetFieldValue<decimal>("ChangePercentage"),
                                    OpenChangePercentage = reader.GetFieldValue<decimal>("OpenChangePercentage"),
                                    CurrencyCode = reader.GetFieldValue<string>("CurrencyCode"),
                                    Name = reader.GetFieldValue<string>("Name"),
                                    YTD = reader.GetFieldValue<decimal>("YTD"),
                                    Week = reader.GetFieldValue<decimal>("Week"),
                                    _2Week = reader.GetFieldValue<decimal>("2Week"),
                                    Month = reader.GetFieldValue<decimal>("Month"),
                                    NoShares = reader.GetFieldValue<decimal>("NoShares"),
                                    MarketCap = reader.GetFieldValue<decimal>("MarketCap"),
                                    _52wHigh = reader.GetFieldValue<decimal>("52wHigh"),
                                    _52wLow = reader.GetFieldValue<decimal>("52wLow"),
                                    _52wChange = reader.GetFieldValue<decimal>("52wChange"),
                                    _3MonthHigh = reader.GetFieldValue<decimal>("3MonthHigh"),
                                    _3MonthLow = reader.GetFieldValue<decimal>("3MonthLow"),
                                    _3MonthChange = reader.GetFieldValue<decimal>("3MonthChange"),
                                    _5YearsChange = reader.GetFieldValue<decimal>("5YearsChange"),
                                    EPS = reader.GetFieldValue<decimal>("EPS"),
                                    SPS = reader.GetFieldValue<double>("SPS"),
                                    DPS = reader.GetFieldValue<decimal>("DPS"),
                                    PayoutRatio = reader.GetFieldValue<double>("PayoutRatio"),
                                    Turnover = reader.GetFieldValue<decimal>("Turnover"),
                                    NetIncome = reader.GetFieldValue<decimal>("NetIncome"),
                                    TurnoverGrowth = reader.GetFieldValue<double>("TurnoverGrowth"),
                                    NetIncomeGrowth = reader.GetFieldValue<double>("NetIncomeGrowth"),
                                    BookValueOfShare = reader.GetFieldValue<double>("BookValueOfShare"),
                                    AllTimeHigh = reader.GetFieldValue<decimal>("AllTimeHigh"),
                                    AllTimeLow = reader.GetFieldValue<decimal>("AllTimeLow"),
                                    TotalMarketCap = reader.GetFieldValue<decimal>("TotalMarketCap"),
                                    PrevMid = reader.GetFieldValue<decimal>("PrevMid"),
                                    _52Highest = reader.GetFieldValue<decimal>("52Highest"),
                                    _52Lowest = reader.GetFieldValue<decimal>("52Lowest"),
                                    HighYTD = reader.GetFieldValue<decimal>("HighYTD"),
                                    LowYTD = reader.GetFieldValue<decimal>("LowYTD"),
                                    Ticker = reader.GetFieldValue<string>("Ticker"),
                                    MarketName = reader.GetFieldValue<string>("MarketName"),
                                    BusinessDaysStoT = reader.GetFieldValue<bool>("BusinessDaysStoT")
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"{DateTime.Now:HH:mm:ss} ERROR: {ex.Message}");
                    }
                }
            }

            return new InstrumentData();
        }
    }
}