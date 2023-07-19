public interface IMyLogger
{
    void LogInformation(string message);

    void LogError(string message);

    void LogWarning(string message);
}

public class MyLogger : IMyLogger
{
    private readonly ILogger<MyLogger> _logger;

    public MyLogger(ILogger<MyLogger> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogError(string message)
    {
        _logger.LogError(message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }
}