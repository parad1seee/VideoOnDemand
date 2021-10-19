using VideoOnDemand.WebSockets.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.WebSockets.Models
{
    public class WSData: IWSItem, IGroupsItem<int>
    {
        public int TokenId { get; set; }

        public int UserId { get; set; }

        public string[] Roles { get; set; } 

        public int[] GroupIds { get; set; }
    }
}
