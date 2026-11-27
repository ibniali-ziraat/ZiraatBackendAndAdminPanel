namespace ECommerce.WebHost
{
    public class SwaggerOAuthOptions
    {
        public string ClientId { get; set; }
        public string AuthorizationUrl { get; set; }
        public string TokenUrl { get; set; }
        public System.Collections.Generic.Dictionary<string, string> Scopes { get; set; } = new();
    }
}
