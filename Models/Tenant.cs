namespace ecommerce.Models
{
    public class Tenant
    {
        public string TenantId { get; set; }
        public string Name { get; set; }

        // Default subdomain (tenant-1.your-saas.com)
        public string SubDomain { get; set; }

        // Client custom domain
        public string? CustomDomain { get; set; }

        public bool IsCustomDomainVerified { get; set; }
    }

}
