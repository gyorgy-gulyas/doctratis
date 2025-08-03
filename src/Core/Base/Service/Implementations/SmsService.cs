using ServiceKit.Net;
using ServiceKit.Net.Communicators;

namespace Core.Base.Service.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly ISmsCommunicator _smsCommunicator;
        public SmsService(ISmsCommunicator smsCommunicator)
        {
            _smsCommunicator = smsCommunicator;
        }

        Task<Response> ISmsService.SendOTP(CallingContext ctx, string phoneNumber, string otp)
        {
            return _smsCommunicator.SendSMS(phoneNumber, $"Docratis bejelentkeési kód: {otp}").AsTask();
        }
    }
}
