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

        public AccessWeekGraphData(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
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
                        Console.WriteLine($"ERROR: {ex.Message}");
                    }
                }
            }

            return weekDataGraphValues;
        }

        public List<decimal> GetPriceForGraph(int instrumentId)
        {
            List<decimal> price = new();
            List<WeekGraphData> weekGraphDataValues = GetWeekDataForGraph(instrumentId);
            foreach (WeekGraphData weekGraphData in weekGraphDataValues)
            {
                price.Add(weekGraphData.Price);
            }

            return price;
        }
    }
}
