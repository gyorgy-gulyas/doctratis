using ServiceKit.Net;
using ServiceKit.Net.Communicators;

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
            return _emailCommunicator.SendEmail("One time password for docratis", $"Docratis bejelentkeési kód: <{otp}>", [emailAddress]);
        }

        public Task<Response> SendEmailConfirmation(CallingContext ctx, string emailAddress, string token, DateTime expiresAt, string guiUrl)
        {
            return _emailCommunicator.SendEmail("Email confirmation for docratis", $"Docratis confirm token kód: <{token}>, érvényes: {expiresAt}, guiUrl:{guiUrl}", [emailAddress]);
        }

        public Task<Response> SendForgotPassword(CallingContext ctx, string emailAddress, string token, DateTime expiresAt)
        {
            return _emailCommunicator.SendEmail("Forgot password docratis", $"Docratis elfejeltett jelszó: <{token}>", [emailAddress]);
        }
    }
}
