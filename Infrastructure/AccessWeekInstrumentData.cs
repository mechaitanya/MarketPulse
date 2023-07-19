using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class WeekData
    {
        public decimal Week_change { get; set; }
        public decimal Week_changepercentage { get; set; }
        public decimal Week_low { get; set; }
        public decimal Week_high { get; set; }
        public long Week_volume { get; set; }
        public DateTime FirstDayOfWeek { get; set; }
        public DateTime LastDayOfWeek { get; set; }
        public string CurrentYear { get; set; }
    }

    public class AccessWeekInstrumentData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

        public AccessWeekInstrumentData(IConfiguration configuration, IMyLogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public async Task<WeekData> GetWeekData(int instrumentID)
        {
            WeekData weekData = new();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectWeekValuesByInstrumentId", sqlConnection))
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
                                weekData.Week_change = reader.GetFieldValue<decimal>("week_change");
                                weekData.Week_changepercentage = reader.GetFieldValue<decimal>("week_changepercentage");
                                weekData.Week_low = reader.GetFieldValue<decimal>("week_low");
                                weekData.Week_high = reader.GetFieldValue<decimal>("week_high");
                                weekData.Week_volume = reader.GetFieldValue<long>("week_volume");
                                weekData.FirstDayOfWeek = reader.GetFieldValue<DateTime>("FirstDayOfWeek");
                                weekData.LastDayOfWeek = reader.GetFieldValue<DateTime>("LastDayOfWeek");
                                weekData.CurrentYear = reader.GetFieldValue<string>("CurrentYear");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERROR: {ex.Message}");
                    }
                }
            }

            return weekData;
        }
    }
}