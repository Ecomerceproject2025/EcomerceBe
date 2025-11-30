using MailKit.Net.Smtp;
using MimeKit;

namespace EcomerceBE.Service
{
    public class Email
    {
        private readonly IConfiguration _config;

        public Email(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Ecomerce App", emailSettings["From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailSettings["Host"], int.Parse(emailSettings["Port"]), false);
            await client.AuthenticateAsync(emailSettings["From"], emailSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

