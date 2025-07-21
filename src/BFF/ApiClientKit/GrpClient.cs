using Grpc.Net.Client;
using Grpc.Core;
using System.Reflection;
using ServiceKit.Net;

namespace BFF.ApiClientKit
{
    static class GrpClient
    {
		static public GrpcChannel _channel;
        static private Metadata _defaultMetadata = new();


        public static void Initialize(string serverAddress, string client_language)
        {
            _channel = GrpcChannel.ForAddress(serverAddress);

            _defaultMetadata.Add(ServiceConstans.const_client_language, client_language);
            _defaultMetadata.Add(ServiceConstans.const_client_application, Assembly.GetExecutingAssembly().GetName().Name);
            _defaultMetadata.Add(ServiceConstans.const_client_version, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            _defaultMetadata.Add(ServiceConstans.const_client_tz_offset, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes.ToString());
            _defaultMetadata.Add(ServiceConstans.const_api_client_kit_version, typeof(GrpClient).Assembly.GetName().Version.ToString());
        }

        public static void SetAuthorization(string userId, string userName)
        {
            var entry = _defaultMetadata.FirstOrDefault( md => md.Key == ServiceConstans.const_calling_user_id);
            if (entry != null)
                _defaultMetadata.Remove(entry);
            _defaultMetadata.Add(ServiceConstans.const_calling_user_id, userId);

            entry = _defaultMetadata.FirstOrDefault( md => md.Key == ServiceConstans.const_calling_user_name);
            if (entry != null)
                _defaultMetadata.Remove(entry);
            _defaultMetadata.Add(ServiceConstans.const_calling_user_name, userName);
        }

        public static void SetAcceptedLanguage(string language)
        {
            var entry = _defaultMetadata.FirstOrDefault( md => md.Key == ServiceConstans.const_client_language);
            if (entry != null)
                _defaultMetadata.Remove(entry);
            _defaultMetadata.Add(ServiceConstans.const_client_language, language);
        }


        public static Metadata GetMetadata(string functionName)
        {
            var merged = new Metadata();
            foreach (var entry in _defaultMetadata)
                merged.Add(entry);

            merged.Add(ServiceConstans.const_call_stack, functionName);

            return merged;   
        }
    }
}