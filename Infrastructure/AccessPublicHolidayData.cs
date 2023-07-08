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
        private readonly ILogger _logger;

        public AccessPublicHolidayData(IConfiguration configuration, ILogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public List<PublicHoliday> SelectAllPublicHolidays(string stringOfInstrumentIds)
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
                        sqlConnection.Open();
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PublicHoliday publicHoliday = new PublicHoliday();
                                publicHoliday.InstrumentId = reader.GetFieldValue<int>("Id");
                                publicHoliday.Fc_ID = reader.GetFieldValue<int>("fc_ID");
                                publicHoliday.Fc_cCode = reader.IsDBNull(reader.GetOrdinal("fc_cCode")) ? null : reader.GetFieldValue<string>("fc_cCode");
                                publicHoliday.Fc_Datetime = reader.GetFieldValue<DateTime>("fc_DateTime");
                                publicHoliday.Fc_Event = reader.GetFieldValue<Int16>("fc_Event");
                                publicHoliday.Fc_Market = reader.GetFieldValue<Int16>("fc_Market");

                                publicHolidays.Add(publicHoliday);
                            }
                            reader.Close();
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