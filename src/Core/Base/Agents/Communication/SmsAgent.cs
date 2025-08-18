using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace Core.Base.Agents.Communication
{
    public class SmsAgent
    {
        private readonly ISmsCommunicator _smsCommunicator;
        public SmsAgent(ISmsCommunicator smsCommunicator)
        {
            _smsCommunicator = smsCommunicator;
        }

        public Task<Response> SendOTP(CallingContext ctx, string phoneNumber, string otp)
        {
            return _smsCommunicator.SendSMS(phoneNumber, $"Docratis bejelentkeési kód: <{otp}>");
        }
    }
}
