using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Infrastructure.Models;
using ECommerce.Module.Core.Models;

namespace ECommerce.Module.WishList.Models
{
    public class WishList : EntityBase
    {
        public WishList()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }

        public long UserId { get; set; }

        public User User { get; set; }

        [StringLength(450)]
        public string SharingCode { get; set; }

        public IList<WishListItem> Items { get; protected set; } = new List<WishListItem>();

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
    }
}
