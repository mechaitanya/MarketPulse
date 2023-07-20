using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace MarketPulse.Models
{
    public record AppSettings(string? SupportEmail, string? SmtpHost, string? ConsumerKey, string? ConsumerSecret, string? ServerFilePath);

    public class AppConfig
    {
        private readonly IConfiguration _configuration;

        public AppConfig(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void GetConfig()
        {
            try
            {
                AppSettings? settings = _configuration.Get<AppSettings>();
                if(settings != null)
                {
                    string? supportEmail = settings.SupportEmail;
                    string? smtpHost = settings.SmtpHost;
                    string? consumerKey = settings.ConsumerKey;
                    string? consumerSecret = settings.ConsumerSecret;
                    string? serverFilePath = settings.ServerFilePath;
                }
                else
                {
                    Console.WriteLine("Configuration settings are not available.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
            }
        }
    }
}