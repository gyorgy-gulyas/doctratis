using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace Core.Test.Mock
{
    public class Mock_SmsCommunicator : ISmsCommunicator
    {
        public record SentSMS(string toPhoneNumber, string messageText);
        public List<SentSMS> sentMessages = [];

        Task<Response> ISmsCommunicator.SendSMS(string toPhoneNumber, string messageText)
        {
            sentMessages.Add(new SentSMS(toPhoneNumber, messageText));
            return Response.Success().AsTask();
        }
    }
}
