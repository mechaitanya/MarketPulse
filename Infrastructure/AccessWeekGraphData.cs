using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class WeekGraphData
    {
        public int InstrumentId { get; set; }
        public DateTime HDate { get; set; }
        public decimal Price { get; set; }
        public int HSize { get; set; }
        public string HTradeLastSellerBuyer { get; set; }
    }

    public class AccessWeekGraphData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _myLogger;

        public AccessWeekGraphData(IConfiguration configuration, IMyLogger myLogger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _myLogger = myLogger;
        }

        public List<WeekGraphData> GetWeekDataForGraph(int instrumentId)
        {
            List<WeekGraphData> weekDataGraphValues = new();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectEndOfWeekDataByInstrumentId", sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentId);
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    try
                    {
                        sqlConnection.Open();
                        SqlDataReader reader = sqlCommand.ExecuteReader();
                        if (reader != null)
                        {
                            while (reader.Read())
                            {
                                WeekGraphData weekDataGraphValue = new()
                                {
                                    HDate = Convert.ToDateTime(reader["hDate"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    HSize = Convert.ToInt32(reader["Size"]),
                                };

                                weekDataGraphValues.Add(weekDataGraphValue);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _myLogger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC for instrument: {instrumentId} in GetWeekDataForGraph");
                    }
                }
            }

            return weekDataGraphValues;
        }

        public List<decimal> GetPriceForGraph(int instrumentId)
        {
            List<decimal> price = new();
            try
            {
                List<WeekGraphData> weekGraphDataValues = GetWeekDataForGraph(instrumentId);
                foreach (WeekGraphData weekGraphData in weekGraphDataValues)
                {
                    price.Add(weekGraphData.Price);
                }
            }
            catch (Exception ex)
            {
                _myLogger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC for instrument: {instrumentId} in GetPriceForGraph");
            }
            return price;
        }
    }
}