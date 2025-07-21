using System.Net.Http.Headers;
using System.Reflection;
using ServiceKit.Net;

namespace BFF.ApiClientKit
{
    static class RestClient
    {
        public static HttpClient HttpClient;

        public static void Initialize(string baseUrl, string client_language)
        {
            HttpClient = new HttpClient();
            HttpClient = new HttpClient() { BaseAddress = new Uri(baseUrl) };
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_client_language, client_language);
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_client_application, Assembly.GetExecutingAssembly().GetName().Name);
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_client_version, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_client_tz_offset, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalMinutes.ToString());
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_api_client_kit_version, typeof(RestClient).Assembly.GetName().Version.ToString());
        }

        public static void SetAuthorization(string bearerToken, string userId, string userName)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            HttpClient.DefaultRequestHeaders.Remove(ServiceConstans.const_calling_user_id);
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_calling_user_id, userId);
            HttpClient.DefaultRequestHeaders.Remove(ServiceConstans.const_calling_user_name);
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_calling_user_name, userName);
        }

        public static void SetAcceptedLanguage(string language)
        {
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
            HttpClient.DefaultRequestHeaders.Remove(ServiceConstans.const_client_language);
            HttpClient.DefaultRequestHeaders.Add(ServiceConstans.const_client_language, language);
        }

        public static Task<HttpResponseMessage> Request(HttpRequestMessage request, string functionName)
        {
            request.Headers.Add("x-request-id", Guid.NewGuid().ToString());
            request.Headers.Add(ServiceConstans.const_call_stack, functionName);

            return HttpClient.SendAsync( request );
        }
    }
}