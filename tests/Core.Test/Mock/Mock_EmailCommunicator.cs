using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace Core.Test.Mock
{
    public class Mock_EmailCommunicator : IEmailCommunicator
    {
        public record SentEmail(string subject, string body, IEnumerable<string> recipients, IEnumerable<IEmailCommunicator.Attachment> attachments);
        public List<SentEmail> sentMessages = [];

        public Task<Response> SendEmail(string subject, string body, IEnumerable<string> recipients, IEnumerable<IEmailCommunicator.Attachment> attachments = null)
        {
            sentMessages.Add(new SentEmail(subject, body, recipients, attachments));
            return Response.Success().AsTask();
        }
    }
}
