using CoreHtmlToImage;
using MarketPulse.Infrastructure;
using ScottPlot;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MarketPulse.Utility
{
    public class CreateImage
    {
        private readonly IConfiguration _configuration;
        private readonly string _serverFilePath;
        private readonly IMyLogger _myLogger;

        public CreateImage(IConfiguration configuration, IMyLogger myLogger)
        {
            _configuration = configuration;
            _serverFilePath = _configuration["serverFilePath"]!;
            _myLogger = myLogger;
        }

        public string GeneratePlot(int instrumentID, string strFileName, string ticker)
        {
            if (!Directory.Exists(Path.Combine(_serverFilePath, ticker)))
            {
                Directory.CreateDirectory(Path.Combine(_serverFilePath, ticker));
            }

            string graphImage = Path.Combine(_serverFilePath, ticker, strFileName + ".png");

            try
            {
                AccessWeekGraphData weekGraph = new(_configuration, _myLogger);
                List<decimal> prices = new();
                try
                {
                    prices = weekGraph.GetPriceForGraph(Convert.ToInt32(instrumentID));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message} at {DateTime.Now} in Generate plot price data fetch");
                }

                if (prices.Count != 0)
                {
                    decimal tolerance = ((prices.Max() / 100) + (prices.Min() / 100)) / 2;
                    double[] x = new double[prices.Count];
                    double[] y = new double[prices.Count];
                    for (int i = 0; i < prices.Count; i++)
                    {
                        x[i] = i + 1;
                        y[i] = (double)prices[i];
                    }

                    var plt = new Plot(365, 201);
                    plt.XAxis.MajorGrid(color: Color.FromArgb(100, Color.Transparent));
                    plt.YAxis.MajorGrid(color: Color.FromArgb(174, 174, 174));
                    plt.XAxis.Line(color: Color.FromArgb(100, Color.Transparent));
                    plt.YAxis.Line(color: Color.FromArgb(100, Color.Transparent));
                    plt.XAxis.TickMarkColor(color: Color.FromArgb(100, Color.Transparent));
                    plt.YAxis.TickMarkColor(color: Color.FromArgb(100, Color.Transparent));
                    plt.XAxis.LabelStyle(color: Color.FromArgb(100, Color.Transparent));
                    plt.YAxis.LabelStyle(color: Color.FromArgb(110, 110, 110), fontName: "OpenSans", fontSize: 10);

                    plt.AddScatter(x, y);
                    plt.SaveFig(graphImage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} @ {DateTime.UtcNow}  UTC in Generate plot");
                _myLogger.LogError($"Error: {ex.Message} at {DateTime.UtcNow} UTC in Generate plot");
            }

            return graphImage;
        }

        public void CreateInteractiveImageWithGraph(int instrumentID, string imagetemplate, string strFileName, string extension, string ticker)
        {
            try
            {
                string graphImageWithPath = GeneratePlot(instrumentID, strFileName, ticker ?? "test");
                imagetemplate = Regex.Replace(imagetemplate, "{graphImagePath}", graphImageWithPath, RegexOptions.IgnoreCase);

                var converter = new HtmlConverter();
                var bytes = converter.FromHtmlString(imagetemplate);

                bool isServerPathExists = Directory.Exists(_serverFilePath);
                if (!isServerPathExists)
                {
                    Directory.CreateDirectory(_serverFilePath);
                }

                bool isExists = Directory.Exists(Path.Combine(_serverFilePath, ticker ?? "test"));
                if (!isExists)
                {
                    Directory.CreateDirectory(Path.Combine(_serverFilePath, ticker ?? "test"));
                }

                File.WriteAllBytes(Path.Combine(_serverFilePath, ticker ?? "test", strFileName + extension), bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} @ {DateTime.UtcNow}  UTC in CreateInteractiveImageWithGraph");
                _myLogger.LogError($"Error: {ex.Message} at {DateTime.UtcNow} UTC in CreateInteractiveImageWithGraph");
            }
        }

        public void CreateInteractiveImage(string imagetemplate, string strFileName, string extension, string ticker)
        {
            try
            {
                bool isServerPathExists = Directory.Exists(_serverFilePath);
                if (!isServerPathExists)
                {
                    Directory.CreateDirectory(_serverFilePath);
                }

                bool isExists = Directory.Exists(Path.Combine(_serverFilePath, ticker ?? "test"));
                if (!isExists)
                {
                    Directory.CreateDirectory(Path.Combine(_serverFilePath, ticker ?? "test"));
                }

                var converter = new HtmlConverter();
                var bytes = converter.FromHtmlString(imagetemplate);
                File.WriteAllBytes(Path.Combine(_serverFilePath, ticker ?? "test", strFileName + extension), bytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} @ {DateTime.UtcNow}  UTC in CreateInteractiveImage");
                _myLogger.LogError($"Error: {ex.Message} at {DateTime.UtcNow} UTC in CreateInteractiveImage");
            }
        }
    }
}