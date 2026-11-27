using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Module.Notifications.Services
{
    public class FirebaseCloudMessagingService : IFirebaseCloudMessagingService
    {
        private const string DefaultAppName = "ECommercePush";
        private const int MulticastBatchSize = 500;

        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly object _lock = new object();
        private FirebaseMessaging _messaging;

        public FirebaseCloudMessagingService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public bool IsConfigured =>
            HasServiceAccountJson() ||
            HasExistingServiceAccountPath();

        public async Task<string> SendSingleMessageAsync(string token, string title, string body, IDictionary<string, string> data = null)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("token/title/body required");
            }

            #pragma warning disable CS0618
            var message = new Message
            {
                Token = token.Trim(),
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = BuildData(data),
                Android = BuildAndroidConfig(),
                Apns = BuildApnsConfig()
            };

            #pragma warning restore CS0618

            return await GetMessaging().SendAsync(message);
        }

        public async Task<string> SendTopicMessageAsync(string topic, string title, string body, IDictionary<string, string> data = null)
        {
            if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("topic/title/body required");
            }

            var message = new Message
            {
                Topic = topic.Trim(),
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = BuildData(data),
                Android = BuildAndroidConfig(),
                Apns = BuildApnsConfig()
            };

            return await GetMessaging().SendAsync(message);
        }

        public async Task<PushNotificationResult> SendMulticastAsync(IEnumerable<string> tokens, string title, string body, IDictionary<string, string> data = null)
        {
            var tokenList = tokens?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList() ?? new List<string>();
            if (tokenList.Count == 0)
            {
                throw new ArgumentException("tokens required");
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("title/body required");
            }

            var responses = new List<PushNotificationSendResponse>();
            var successCount = 0;
            var failureCount = 0;

            for (var offset = 0; offset < tokenList.Count; offset += MulticastBatchSize)
            {
                var batch = tokenList.GetRange(offset, Math.Min(MulticastBatchSize, tokenList.Count - offset));

                #pragma warning disable CS0618
                var message = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = BuildData(data),
                    Android = BuildAndroidConfig(),
                    Apns = BuildApnsConfig()
                };

                #pragma warning restore CS0618

                var batchResult = await GetMessaging().SendEachForMulticastAsync(message);
                successCount += batchResult.SuccessCount;
                failureCount += batchResult.FailureCount;
                responses.AddRange(batchResult.Responses.Select(x => new PushNotificationSendResponse
                {
                    Success = x.IsSuccess,
                    MessageId = x.MessageId,
                    Error = x.Exception?.Message
                }));
            }

            return new PushNotificationResult
            {
                Success = failureCount == 0,
                SuccessCount = successCount,
                FailureCount = failureCount,
                Responses = responses
            };
        }

        private FirebaseMessaging GetMessaging()
        {
            if (_messaging != null)
            {
                return _messaging;
            }

            lock (_lock)
            {
                if (_messaging != null)
                {
                    return _messaging;
                }

                if (!IsConfigured)
                {
                    throw new InvalidOperationException("Firebase service account is not configured or the configured file path does not exist.");
                }

                var appName = _configuration["Firebase:AppName"];
                if (string.IsNullOrWhiteSpace(appName))
                {
                    appName = DefaultAppName;
                }

                var app = GetExistingApp(appName) ?? FirebaseApp.Create(new AppOptions
                {
                    Credential = GetCredential()
                }, appName);

                _messaging = FirebaseMessaging.GetMessaging(app);
                return _messaging;
            }
        }

        private static FirebaseApp GetExistingApp(string appName)
        {
            try
            {
                return FirebaseApp.GetInstance(appName);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private GoogleCredential GetCredential()
        {
            var serviceAccountJson = GetServiceAccountJson();
            if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                return CredentialFactory
                    .FromJson<ServiceAccountCredential>(serviceAccountJson)
                    .ToGoogleCredential();
            }

            var path = GetResolvedServiceAccountPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("Firebase service account path is not configured.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Firebase service account file was not found.", path);
            }

            return CredentialFactory
                .FromFile<ServiceAccountCredential>(path)
                .ToGoogleCredential();
        }

        private bool HasServiceAccountJson()
        {
            return !string.IsNullOrWhiteSpace(GetServiceAccountJson());
        }

        private bool HasExistingServiceAccountPath()
        {
            var path = GetResolvedServiceAccountPath();
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private string GetServiceAccountJson()
        {
            return _configuration["Firebase:ServiceAccountJson"];
        }

        private string GetServiceAccountPath()
        {
            return _configuration["Firebase:ServiceAccountPath"];
        }

        private string GetResolvedServiceAccountPath()
        {
            var path = GetServiceAccountPath();
            if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(_environment.ContentRootPath, path);
        }

        private static IReadOnlyDictionary<string, string> BuildData(IDictionary<string, string> data)
        {
            var payload = data == null
                ? new Dictionary<string, string>()
                : data
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value != null)
                    .ToDictionary(x => x.Key, x => x.Value);

            payload["sent_at"] = DateTimeOffset.UtcNow.ToString("O");
            return payload;
        }

        private AndroidConfig BuildAndroidConfig()
        {
            var channelId = _configuration["Firebase:AndroidChannelId"];
            return new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    Sound = "default",
                    ChannelId = string.IsNullOrWhiteSpace(channelId) ? null : channelId.Trim()
                }
            };
        }

        private static ApnsConfig BuildApnsConfig()
        {
            return new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = "10",
                    ["apns-push-type"] = "alert"
                },
                Aps = new Aps
                {
                    Sound = "default",
                    ContentAvailable = true
                }
            };
        }
    }
}
