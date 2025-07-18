namespace BFF.ApiClient
{
    static class Rest
    {
        public static HttpClient HttpClient;

        public static void Initialize(string baseUrl)
        {
            HttpClient = new HttpClient();
            HttpClient = new HttpClient() { BaseAddress = new Uri(baseUrl) };
            //HttpClient.DefaultRequestHeaders.Add( PrivateHeaderValues.AppGatewayVersion, typeof( ApiGatewayClient ).Assembly.GetName().Version.ToString() );
            //Client.DefaultRequestHeaders.Add( PrivateHeaderValues.ClientType, clientType );
            //Client.DefaultRequestHeaders.Add( PrivateHeaderValues.ClientVersion, clientVersion );
            //Client.DefaultRequestHeaders.Add( PrivateHeaderValues.ClientTimeZoneOffset, TimeZoneInfo.Local.GetUtcOffset( DateTime.UtcNow ).TotalMinutes.ToString() );            
        }
    }
}