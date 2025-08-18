using ServiceKit.Net;
using ServiceKit.Net.Communicators;
using Twilio.Jwt.AccessToken;

namespace Core.Base.Agents.Communication
{
    public class EmailAgent
    {
        private readonly IEmailCommunicator _emailCommunicator;
        public EmailAgent(IEmailCommunicator emailCommunicator)
        {
            _emailCommunicator = emailCommunicator;
        }

        public Task<Response> SendOTP(CallingContext ctx, string emailAddress, string otp)
        {
            return _emailCommunicator.SendEmail("one time otp for docratis", $"Docratis bejelentkeési kód: <{otp}>", [emailAddress]);
        }

        public Task<Response> SendEmailConfirmation(CallingContext ctx, string emailAddress, string token, DateTime expiresAt, string guiUrl)
        {
            return _emailCommunicator.SendEmail("Email confirmation for docratis", $"Docratis confirm token kód: <{token}>, érvényes: {expiresAt}, guiUrl:{guiUrl}", [emailAddress]);
        }
    }
}
