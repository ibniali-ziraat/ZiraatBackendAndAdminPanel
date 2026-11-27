using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Infrastructure.Data;
using ECommerce.Module.Core.Areas.Core.ViewModels;
using ECommerce.Module.Core.Models;
using Microsoft.Extensions.Options;
namespace ECommerce.Module.Core.Services
{
    public interface IOtpVerificationService
    {
        Task<OtpVerification> GetByIdAsync(long id);
        Task<OtpVerification> GetByPhoneNumberAsync(string phoneNumber);
        Task<IEnumerable<OtpVerification>> GetAllAsync();
        Task CreateAsync(OtpVerificationViewModel model);
        Task UpdateAsync(long id, OtpVerificationViewModel model);
        Task DeleteAsync(long id);

        // OTP-specific methods (use phone number instead of profile id)
        Task<string> SendOtpAsync(string mobileNumber);
        string VerifyOtp(string mobileNumber, string otp);
    }

    public class OtpVerificationService : IOtpVerificationService
    {
        private readonly IRepository<OtpVerification> _repo;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<OtpVerificationService> _logger;
        private readonly Fast2SmsSettings _settings;
        private readonly HttpClient _client = new HttpClient();
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();
        public OtpVerificationService(IRepository<OtpVerification> repo, ISmsSender smsSender, ILogger<OtpVerificationService> logger, IOptions<Fast2SmsSettings> settings)
        {
            _repo = repo;
            _smsSender = smsSender;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<OtpVerification> GetByIdAsync(long id)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<OtpVerification> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _repo.Query().FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        }

        public async Task<IEnumerable<OtpVerification>> GetAllAsync()
        {
            return await _repo.Query().ToListAsync();
        }

        public async Task CreateAsync(OtpVerificationViewModel model)
        {
            var entity = new OtpVerification
            {
                PhoneNumber = model.PhoneNumber,
                Otp = model.Otp,
                GeneratedOn = DateTime.UtcNow,
                IsVerified = false
            };

            _repo.Add(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(long id, OtpVerificationViewModel model)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            entity.PhoneNumber = model.PhoneNumber;
            entity.Otp = model.Otp;
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _repo.Query().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return;

            _repo.Remove(entity);
            await _repo.SaveChangesAsync();
        }

        // Generates a secure 6-digit numeric OTP
        //private static string GenerateOtp()
        //{
        //    using var rng = RandomNumberGenerator.Create();
        //    var bytes = new byte[4];
        //    rng.GetBytes(bytes);
        //    var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        //    return value.ToString("D6");
        //}

        // Create or update the OTP record and attempt to send SMS via ISmsSender.
        //public async Task<bool> SendOtpAsync(string phoneNumber)
        //{
        //    if (string.IsNullOrWhiteSpace(phoneNumber))
        //    {
        //        _logger.LogWarning("SendOtpAsync called without phone number.");
        //        return false;
        //    }

        //    var otp = GenerateOtp();

        //    var entity = await _repo.Query().FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber);
        //    if (entity == null)
        //    {
        //        entity = new OtpVerification
        //        {
        //            PhoneNumber = phoneNumber,
        //            Otp = otp,
        //            GeneratedOn = DateTime.UtcNow,
        //            IsVerified = false
        //        };
        //        _repo.Add(entity);
        //    }
        //    else
        //    {
        //        entity.Otp = otp;
        //        entity.GeneratedOn = DateTime.UtcNow;
        //        entity.IsVerified = false;
        //    }

        //    await _repo.SaveChangesAsync();

        //    var message = $"Your verification code is {otp}";
        //    try
        //    {
        //        await _smsSender.SendSmsAsync(phoneNumber, message);
        //        _logger.LogInformation("OTP generated and SMS sent to {Phone}", phoneNumber);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to send OTP SMS to {Phone}", phoneNumber);
        //        return false;
        //    }
        //}

        // Verifies the OTP for the given phone number. Marks as verified on success.
        // Uses a default validity window of 5 minutes.

        public string GenerateOtp()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString();
        }

        public async Task<string> SendOtpAsync(string mobileNumber)
        {
            string otp = GenerateOtp();
            DateTime expiry = DateTime.Now.AddMinutes(_settings.OtpExpiryMinutes);

            _otpStore[mobileNumber] = (otp, expiry);

            var url = _settings.Url;
            var payload = new StringContent(
                $"route={_settings.Route}&sender_id={_settings.SenderId}&message={_settings.MessageTemplateId}&variables_values={otp}&flash={_settings.Flash}&numbers={mobileNumber}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("authorization", _settings.ApiKey);

            HttpResponseMessage response = await _client.PostAsync(url, payload);
            return await response.Content.ReadAsStringAsync();
        }

        public  string VerifyOtp(string mobileNumber, string userOtp)
        {
            if (!_otpStore.ContainsKey(mobileNumber))
                return "No OTP found for this number";

            var (otp, expiry) = _otpStore[mobileNumber];

            if (DateTime.Now > expiry)
            {
                _otpStore.TryRemove(mobileNumber, out _);
                return "OTP expired";
            }

            if (userOtp == otp)
            {
                _otpStore.TryRemove(mobileNumber, out _);
                return "OTP verified successfully";
            }

            return "Invalid OTP";
        }
    }
}
