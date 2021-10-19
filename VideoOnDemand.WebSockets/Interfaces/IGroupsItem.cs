using System;
using System.Collections.Generic;
using System.Text;

namespace VideoOnDemand.WebSockets.Interfaces
{
    public interface IGroupsItem<Key>
    {
        Key[] GroupIds { get; set; }
    }
}
