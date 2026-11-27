using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;

namespace ECommerce.Module.Core.Models;

// Profession.cs
public class Profession : EntityBase
{
    public Profession() { }

    public Profession(long id)
    {
        Id = id;
    }

    public long Id { get; set; }
    public long ProfileId { get; set; }
    public string ProfessionType { get; set; }
    public string OrganizationName { get; set; }
    public string Designation { get; set; }
    public int ExperienceYears { get; set; }
    public string Skills { get; set; }
}



