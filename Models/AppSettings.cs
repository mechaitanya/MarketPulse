using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

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
            AppSettings settings = _configuration.Get<AppSettings>();

            string supportEmail = settings.SupportEmail;
            string smtpHost = settings.SmtpHost;
            string consumerKey = settings.ConsumerKey;
            string consumerSecret = settings.ConsumerSecret;
            string serverFilePath = settings.ServerFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
        }
    }
}

public class AppSettings
{
    public string? SupportEmail { get; set; }
    public string? SmtpHost { get; set; }
    public string? ConsumerKey { get; set; }
    public string? ConsumerSecret { get; set; }
    public string? ServerFilePath { get; set; }
}

