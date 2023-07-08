using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class AccessTimeZoneData
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public AccessTimeZoneData(IConfiguration configuration, ILogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public string GetTimezone(int instrumentId)
        {
            string timezone;

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulseSelectTimezoneByInstrumentId", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentId);

                    try
                    {
                        sqlConnection.Open();
                        timezone = (string)sqlCommand.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation($"ERROR: {ex.Message} at {DateTime.Now:HH:mm:ss}");
                        throw;
                    }
                }
            }

            return timezone;
        }

        public DateTime GetDayLightSavingTime(int instrumentId, DateTime tweetTime)
        {
            try
            {
                TimeZoneInfo zone = TimeZoneInfo.Local;
                string standardName = zone.StandardName;
                bool isDayLight = TimeZoneInfo.FindSystemTimeZoneById(standardName).IsDaylightSavingTime(tweetTime);
                string instrumentTimezone = GetTimezone(instrumentId) ?? standardName;
                bool isDayLightOfInstrument = TimeZoneInfo.FindSystemTimeZoneById(instrumentTimezone).IsDaylightSavingTime(tweetTime);
                double value = 0;

                if (isDayLight && !isDayLightOfInstrument)
                {
                    value = 1;
                }
                else if (!isDayLight && isDayLightOfInstrument)
                {
                    value = -1;
                }

                tweetTime = tweetTime.AddHours(value);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR: {ex.Message} at {DateTime.Now:HH:mm:ss}, InstrumentId: {instrumentId}, Problem in accessing SQL Server database, procedure named spMPulseSelectTimezoneByInstrumentId");
            }

            return tweetTime;
        }
    }
}