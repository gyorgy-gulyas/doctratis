using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace Core.Base.Service.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IEmailCommunicator _emailCommunicator;
        public EmailService(IEmailCommunicator emailCommunicator)
        {
            _emailCommunicator = emailCommunicator;
        }

        Task<Response> IEmailService.SendOTP(CallingContext ctx, string emailAddress, string otp)
        {
            return _emailCommunicator.SendEmail("one time otp for docratis", $"Docratis bejelentkeési kód: {otp}", [emailAddress]);
        }
    }
}
