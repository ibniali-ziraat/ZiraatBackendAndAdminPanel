using System;

namespace ECommerce.Module.Core.Areas.Core.ViewModels;

public class Fast2SmsSettings
{
    public string Url { get; set; }
    public string ApiKey { get; set; }
    public string Route { get; set; }
    public string SenderId { get; set; }
    public string MessageTemplateId { get; set; }
    public string Flash { get; set; }
    public int OtpExpiryMinutes { get; set; }
}
