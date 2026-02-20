using Nexora.Web.Data.Entities;

namespace Nexora.Web.Data.Models
{
    public class Subscription : BaseEntity
    {

        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; } = default!;

        public Guid PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; } = default!;

        public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndDateUtc { get; set; }

        public bool IsActive { get; set; } = true;
        public bool AutoRenew { get; set; } = true;
    }

}
