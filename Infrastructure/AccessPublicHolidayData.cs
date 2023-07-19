using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class PublicHoliday
    {
        public int InstrumentId { get; set; }
        public int Fc_ID { get; set; }
        public string Fc_cCode { get; set; }
        public DateTime Fc_Datetime { get; set; }
        public Int16 Fc_Event { get; set; }
        public Int16 Fc_Market { get; set; }
    }

    public class AccessPublicHolidayData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

        public AccessPublicHolidayData(IConfiguration configuration, IMyLogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public async Task<List<PublicHoliday>> SelectAllPublicHolidaysAsync(string stringOfInstrumentIds)
        {
            List<PublicHoliday> publicHolidays = new();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectAllPublicHolidaysByInstrumentIds", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StringInstrumentIds", stringOfInstrumentIds);

                    try
                    {
                        await sqlConnection.OpenAsync();

                        using (SqlDataReader reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                PublicHoliday publicHoliday = new()
                                {
                                    InstrumentId = reader.GetFieldValue<int>("Id"),
                                    Fc_ID = reader.GetFieldValue<int>("fc_ID"),
                                    Fc_cCode = reader.IsDBNull(reader.GetOrdinal("fc_cCode")) ? null : reader.GetFieldValue<string>("fc_cCode"),
                                    Fc_Datetime = reader.GetFieldValue<DateTime>("fc_DateTime"),
                                    Fc_Event = reader.GetFieldValue<Int16>("fc_Event"),
                                    Fc_Market = reader.GetFieldValue<Int16>("fc_Market")
                                };

                                publicHolidays.Add(publicHoliday);
                            }

                            await reader.CloseAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"ERROR: {ex.Message} at {DateTime.Now:HH:mm:ss}");
                    }
                }
            }
            return publicHolidays;
        }

        public bool CheckPublicHoliday(long instrumentId, DateTime date, List<PublicHoliday> lsPublicHoliday)
        {
            lsPublicHoliday = lsPublicHoliday.Where(x => x.Fc_Datetime.Date == date.Date && x.InstrumentId == instrumentId).ToList();

            bool isPublicHoliday = lsPublicHoliday.Count > 0;

            return isPublicHoliday;
        }

        public string GetWeekendDays(int instrumentId)
        {
            string weekendDays;

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectBusinessDaysByInstrumentId", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentId);

                    try
                    {
                        sqlConnection.Open();
                        bool businessDaysStoT = (bool)sqlCommand.ExecuteScalar();
                        weekendDays = businessDaysStoT ? "56" : "67";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"ERROR: {ex.Message} at {DateTime.Now:HH:mm:ss}");
                        throw;
                    }
                }
            }

            return weekendDays;
        }
    }
}