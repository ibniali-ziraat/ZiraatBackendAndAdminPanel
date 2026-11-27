using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

public class OtpVerification: IEntityWithTypedId<long>
{
    public long Id { get; set; }

    // Replaced ProfileId with PhoneNumber
    public string PhoneNumber { get; set; }

    public string Otp { get; set; }
    public DateTime GeneratedOn { get; set; }
    public bool IsVerified { get; set; }
}
