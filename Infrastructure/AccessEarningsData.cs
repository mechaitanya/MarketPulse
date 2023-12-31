﻿using Microsoft.Data.SqlClient;
using System.Data;

namespace MarketPulse.Infrastructure
{
    public class Earning
    {
        public int E_InstrumentId { get; set; }
        public string E_EventName { get; set; }
        public DateTime E_Date { get; set; }
    }

    public class AccessEarningsData
    {
        private readonly string _connectionString;
        private readonly IMyLogger _logger;

        public AccessEarningsData(IConfiguration configuration, IMyLogger logger)
        {
            _connectionString = configuration.GetConnectionString("SharkSiteConnectionString");
            _logger = logger;
        }

        public async Task<List<Earning>> GetEarningList(string stringOfInstrumentIds)
        {
            List<Earning> earnings = new();

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                using (var sqlCommand = new SqlCommand("[spMPulseSelectFinCalendarEarningDatesByInstrumentIds]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@StringInstrumentIds", stringOfInstrumentIds);

                    try
                    {
                        await sqlConnection.OpenAsync();
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Earning earning = new()
                                {
                                    E_InstrumentId = reader.GetFieldValue<int>("Id"),
                                    E_EventName = reader.GetFieldValue<string>("EventName"),
                                    E_Date = reader.GetFieldValue<DateTime>("Date")
                                };
                                earnings.Add(earning);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ERROR: {ex.Message} in GetEarningList at {DateTime.UtcNow:HH:mm:ss} UTC");
                    }
                }
            }
            return earnings;
        }
    }
}