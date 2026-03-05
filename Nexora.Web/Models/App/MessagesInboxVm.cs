using Nexora.Web.Data.Entities;
using Nexora.Web.Data.Models;

namespace Nexora.Web.Models.App;

public class MessagesInboxVm
{
    public List<ContactMessage> ContactMessages { get; set; } = new();
    public int UnreadContactCount { get; set; }

    public List<Product> LowStockProducts { get; set; } = new();
}
