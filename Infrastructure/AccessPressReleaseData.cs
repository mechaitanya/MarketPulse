using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class PressRelease
    {
        public long PR_ID { get; set; }
        public string PR_Title { get; set; }
        public DateTime PR_Date { get; set; }
        public DateTime PR_ServerDate { get; set; }
        public long PR_Instrument_ID { get; set; }
        public string PR_Language { get; set; }
        public string PR_MessageType { get; set; }
        public string PR_Link { get; set; }
    }

    public class AccessPressReleaseData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

        public AccessPressReleaseData(IConfiguration configuration, IMyLogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public List<PressRelease> GetPressReleaseList(int instrumentId, string aLanguages, string aSourceIds)
        {
            List<PressRelease> pressReleases = new();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("spMPulsePressReleaseSelectByInstrumentIdAndMultipleSouceID", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@InstrumentId", instrumentId);
                    sqlCommand.Parameters.AddWithValue("@Languages", aLanguages);
                    sqlCommand.Parameters.AddWithValue("@SourceId", aSourceIds);

                    try
                    {
                        sqlConnection.Open();
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                PressRelease pressRelease = new()
                                {
                                    PR_ID = reader.GetFieldValue<long>("PressReleaseID"),
                                    PR_Title = reader.GetFieldValue<string>("Title"),
                                    PR_Date = reader.GetFieldValue<DateTime>("Date"),
                                    PR_ServerDate = reader.GetFieldValue<DateTime>("ServerDate").AddMinutes(-2),
                                    PR_Instrument_ID = Convert.ToInt64(reader.GetFieldValue<int>("InstrumentID")),
                                    PR_Language = reader.GetFieldValue<string>("Language"),
                                    PR_MessageType = reader.GetFieldValue<int>("MessageType").ToString(),
                                    PR_Link = reader.GetFieldValue<string>("Link")
                                };
                                pressReleases.Add(pressRelease);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC in GetPressReleaseList");
                    }
                }
            }
            return pressReleases;
        }

        public void UpdateTweetedPressReleases(long? prID)
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("[dbo].[spMPulseUpdateTweetedPressReleases]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@PRID", prID);

                    try
                    {
                        sqlConnection.Open();
                        sqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ERROR: {ex.Message} at {DateTime.UtcNow:HH:mm:ss} UTC in UpdateTweetedPressReleases");
                    }
                }
            }
        }
    }
}