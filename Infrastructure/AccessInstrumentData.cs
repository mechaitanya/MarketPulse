using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class AccessInstrumentData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

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

        public AccessInstrumentData(IConfiguration configuration, IMyLogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public async Task<InstrumentData> GetPrice(int instrumentID)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectInstrumentDataById", sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentID);
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        await sqlConnection.OpenAsync();
                        using (var reader = sqlCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new InstrumentData
                                {
                                    InstrumentId = (int)reader["InstrumentId"],
                                    Bid = reader.IsDBNull("Bid") ? default : reader.GetFieldValue<decimal>("Bid"),
                                    Ask = reader.IsDBNull("Ask") ? default : reader.GetFieldValue<decimal>("Ask"),
                                    Open = reader.IsDBNull("Open") ? default : reader.GetFieldValue<decimal>("Open"),
                                    Last = reader.IsDBNull("Last") ? default : reader.GetFieldValue<decimal>("Last"),
                                    High = reader.IsDBNull("High") ? default : reader.GetFieldValue<decimal>("High"),
                                    Low = reader.IsDBNull("Low") ? default : reader.GetFieldValue<decimal>("Low"),
                                    Volume = reader.IsDBNull("Volume") ? default : reader.GetFieldValue<long>("Volume"),
                                    Mid = reader.IsDBNull("Mid") ? default : reader.GetFieldValue<decimal>("Mid"),
                                    Date = reader.IsDBNull("Date") ? default : reader.GetFieldValue<DateTime>("Date"),
                                    PrevClose = reader.IsDBNull("PrevClose") ? default : reader.GetFieldValue<decimal>("PrevClose"),
                                    Change = reader.IsDBNull("Change") ? default : reader.GetFieldValue<decimal>("Change"),
                                    OpenChange = reader.IsDBNull("OpenChange") ? default : reader.GetFieldValue<decimal>("OpenChange"),
                                    LastRowChange = reader.IsDBNull("LastRowChange") ? default : reader.GetFieldValue<DateTime>("LastRowChange"),
                                    ChangePercentage = reader.IsDBNull("ChangePercentage") ? default : reader.GetFieldValue<decimal>("ChangePercentage"),
                                    OpenChangePercentage = reader.IsDBNull("OpenChangePercentage") ? default : reader.GetFieldValue<decimal>("OpenChangePercentage"),
                                    CurrencyCode = reader.IsDBNull("CurrencyCode") ? null : reader.GetFieldValue<string>("CurrencyCode"),
                                    Name = reader.IsDBNull("Name") ? null : reader.GetFieldValue<string>("Name"),
                                    YTD = reader.IsDBNull("YTD") ? default : reader.GetFieldValue<decimal>("YTD"),
                                    Week = reader.IsDBNull("Week") ? default : reader.GetFieldValue<decimal>("Week"),
                                    _2Week = reader.IsDBNull("2Week") ? default : reader.GetFieldValue<decimal>("2Week"),
                                    Month = reader.IsDBNull("Month") ? default : reader.GetFieldValue<decimal>("Month"),
                                    NoShares = reader.IsDBNull("NoShares") ? default : reader.GetFieldValue<decimal>("NoShares"),
                                    MarketCap = reader.IsDBNull("MarketCap") ? default : reader.GetFieldValue<decimal>("MarketCap"),
                                    _52wHigh = reader.IsDBNull("52wHigh") ? default : reader.GetFieldValue<decimal>("52wHigh"),
                                    _52wLow = reader.IsDBNull("52wLow") ? default : reader.GetFieldValue<decimal>("52wLow"),
                                    _52wChange = reader.IsDBNull("52wChange") ? default : reader.GetFieldValue<decimal>("52wChange"),
                                    _3MonthHigh = reader.IsDBNull("3MonthHigh") ? default : reader.GetFieldValue<decimal>("3MonthHigh"),
                                    _3MonthLow = reader.IsDBNull("3MonthLow") ? default : reader.GetFieldValue<decimal>("3MonthLow"),
                                    _3MonthChange = reader.IsDBNull("3MonthChange") ? default : reader.GetFieldValue<decimal>("3MonthChange"),
                                    _5YearsChange = reader.IsDBNull("5YearsChange") ? default : reader.GetFieldValue<decimal>("5YearsChange"),
                                    EPS = reader.IsDBNull("EPS") ? default : reader.GetFieldValue<decimal>("EPS"),
                                    SPS = reader.IsDBNull("SPS") ? default : reader.GetFieldValue<double>("SPS"),
                                    DPS = reader.IsDBNull("DPS") ? default : reader.GetFieldValue<decimal>("DPS"),
                                    PayoutRatio = reader.IsDBNull("PayoutRatio") ? default : reader.GetFieldValue<double>("PayoutRatio"),
                                    Turnover = reader.IsDBNull("Turnover") ? default : reader.GetFieldValue<decimal>("Turnover"),
                                    NetIncome = reader.IsDBNull("NetIncome") ? default : reader.GetFieldValue<decimal>("NetIncome"),
                                    TurnoverGrowth = reader.IsDBNull("TurnoverGrowth") ? default : reader.GetFieldValue<double>("TurnoverGrowth"),
                                    NetIncomeGrowth = reader.IsDBNull("NetIncomeGrowth") ? default : reader.GetFieldValue<double>("NetIncomeGrowth"),
                                    BookValueOfShare = reader.IsDBNull("BookValueOfShare") ? default : reader.GetFieldValue<double>("BookValueOfShare"),
                                    AllTimeHigh = reader.IsDBNull("AllTimeHigh") ? default : reader.GetFieldValue<decimal>("AllTimeHigh"),
                                    AllTimeLow = reader.IsDBNull("AllTimeLow") ? default : reader.GetFieldValue<decimal>("AllTimeLow"),
                                    TotalMarketCap = reader.IsDBNull("TotalMarketCap") ? default : reader.GetFieldValue<decimal>("TotalMarketCap"),
                                    PrevMid = reader.IsDBNull("PrevMid") ? default : reader.GetFieldValue<decimal>("PrevMid"),
                                    _52Highest = reader.IsDBNull("52Highest") ? default : reader.GetFieldValue<decimal>("52Highest"),
                                    _52Lowest = reader.IsDBNull("52Lowest") ? default : reader.GetFieldValue<decimal>("52Lowest"),
                                    HighYTD = reader.IsDBNull("HighYTD") ? default : reader.GetFieldValue<decimal>("HighYTD"),
                                    LowYTD = reader.IsDBNull("LowYTD") ? default : reader.GetFieldValue<decimal>("LowYTD"),
                                    Ticker = reader.IsDBNull("Ticker") ? null : reader.GetFieldValue<string>("Ticker"),
                                    MarketName = reader.IsDBNull("MarketName") ? null : reader.GetFieldValue<string>("MarketName"),
                                    BusinessDaysStoT = !reader.IsDBNull("BusinessDaysStoT") && reader.GetFieldValue<bool>("BusinessDaysStoT")
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC in GetPrice.");
                    }
                }
            }

            return new InstrumentData();
        }
    }
}