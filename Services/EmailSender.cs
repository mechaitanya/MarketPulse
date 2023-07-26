using System.Net.Mail;

namespace MarketPulse.Services
{
    public interface IEmailSender
    {
        void SendAuthorizationFailedEmail(string clientUserId);
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly IMyLogger _myLogger;

        public EmailSender(IConfiguration configuration, IMyLogger myLogger)
        {
            _configuration = configuration;
            _myLogger = myLogger;
        }

        public void SendAuthorizationFailedEmail(string userID)
        {
            try
            {
                string toEmail = _configuration["supportEmail"]!;
                string smtpHost = _configuration["smtpHost"]!;

                MailMessage mail = new("noreply@euroland.com", toEmail);
                SmtpClient smtpClient = new()
                {
                    Port = 25,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = smtpHost
                };
                mail.IsBodyHtml = true;
                mail.Subject = "Client Authorization failed on Market Pulse.";
                mail.Body = $"Dear Admin,<br/><br/>A twitter user {userID} has not authorized the Market Pulse App yet or has revoked "
                             + "the access of his/her account of Market Pulse Application."
                             + "<br>Please advise client to re-authorize his / her acccount again on web application."
                             + "<br/><br/>Regards.";
                smtpClient.Send(mail);
            }
            catch (Exception ex)
            {
                _myLogger.LogError($"Error sending mail at {DateTime.UtcNow.ToShortDateString()} UTC: " + ex.ToString());
            }
        }
    }
}