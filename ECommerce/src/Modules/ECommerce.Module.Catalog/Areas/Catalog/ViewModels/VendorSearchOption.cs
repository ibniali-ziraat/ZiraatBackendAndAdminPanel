using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Module.Catalog.Areas.Catalog.ViewModels;


public class VendorSearchOption : SearchOption
    {
        public long? VendorId { get; set; }
        public string VendorSlug { get; set; }
    }
