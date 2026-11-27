using System;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

public class Profile : EntityBase, IEntityWithTypedId<long>
{
    // Add a public parameterless constructor to fix CS0122
    public Profile() { }

    public long Id { get; set; }
    public long CoreUserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int? Age { get; set; }
    public string Gender { get; set; }
    public string ProfilePic { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public DateTime CreatedOn { get; set; }
}
