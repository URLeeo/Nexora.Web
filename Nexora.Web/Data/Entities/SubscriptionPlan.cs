using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class SubscriptionPlan : BaseEntity
    {
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal MonthlyPrice { get; set; }
        public int? MaxUsers { get; set; }
        public int? MaxProducts { get; set; }
    }
}
