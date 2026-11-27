using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Module.Core.Services
{
    public class SmsSender : ISmsSender
    {
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _twilioFromNumber;

        public SmsSender(IConfiguration config)
        {
            _twilioAccountSid = config.GetValue<string>("Twilio:AccountSid");
            _twilioAuthToken = config.GetValue<string>("Twilio:AuthToken");
            _twilioFromNumber = config.GetValue<string>("Twilio:From");
        }

        public async Task SendSmsAsync(string number, string message)
        {
            // If Twilio is not configured, do nothing (development fallback).
            if (string.IsNullOrWhiteSpace(_twilioAccountSid) ||
                string.IsNullOrWhiteSpace(_twilioAuthToken) ||
                string.IsNullOrWhiteSpace(_twilioFromNumber))
            {
                // In development you can log the message or inspect it in debug.
                return;
            }

            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_twilioAccountSid}/Messages.json";

            using var client = new HttpClient();
            var authBytes = Encoding.ASCII.GetBytes($"{_twilioAccountSid}:{_twilioAuthToken}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("To", number),
                new KeyValuePair<string, string>("From", _twilioFromNumber),
                new KeyValuePair<string, string>("Body", message)
            };

            using var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
        }
    }
}
