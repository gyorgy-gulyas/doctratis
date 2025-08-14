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
            return _emailCommunicator.SendEmail("one time otp for docratis", $"Docratis bejelentkeési kód: {otp}", [emailAddress]);
        }
    }
}
