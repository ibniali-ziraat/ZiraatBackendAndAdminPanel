using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Module.Core.Areas.Core.ViewModels;

public class ProfessionViewModel
{
    public long ProfileId { get; set; }
    public string ProfessionType { get; set; }
    public string OrganizationName { get; set; }
    public string Designation { get; set; }
    public int ExperienceYears { get; set; }
    public string Skills { get; set; }
}
