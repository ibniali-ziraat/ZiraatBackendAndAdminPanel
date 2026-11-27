using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

public class UserProfileAddress : EntityBase
{
    // Id is inherited from EntityBase (long Id)

    public long ProfileId { get; set; }

    public string Village { get; set; }

    public string Tehsil { get; set; }

    public string District { get; set; }

    public string State { get; set; }

    public string Pincode { get; set; }

    public string Gps { get; set; }
}
