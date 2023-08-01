using Microsoft.Data.SqlClient;
using System;
using System.Data;
using TimeZoneConverter;

namespace MarketPulse.Infrastructure
{
    public class AccessTimeZoneData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

        public AccessTimeZoneData(IConfiguration configuration, IMyLogger logger)
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
                        _logger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC for instrument: {instrumentId} in GetTimezone");
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
                TimeZoneInfo localZone = TimeZoneInfo.Local;
                bool isDayLight = localZone.IsDaylightSavingTime(tweetTime);
                string instrumentTimezone = GetTimezone(instrumentId);
                if (string.IsNullOrEmpty(instrumentTimezone))
                {
                    instrumentTimezone = localZone.Id; 
                }

                TimeZoneInfo instrumentZone = TZConvert.GetTimeZoneInfo(instrumentTimezone);
                bool isDayLightOfInstrument = instrumentZone.IsDaylightSavingTime(tweetTime);

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
                Console.WriteLine($"{ex.Message} @ {DateTime.UtcNow} UTC in GetDayLightSavingTime");
                _logger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss}, InstrumentId: {instrumentId} in GetDayLightSavingTime");
            }
            return tweetTime;
        }

    }
}