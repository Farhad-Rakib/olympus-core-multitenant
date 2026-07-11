using System.Collections.Generic;
using OlympusCoreMultitenant.Domain.Common;

namespace OlympusCoreMultitenant.Domain.Entities
{
    public class Menu : BaseEntity, ITenantEntity
    {
        public long TenantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public string? RequiredPermission { get; set; }
        public long? ParentMenuId { get; set; }
        public Menu? ParentMenu { get; set; }
        public List<Menu>? Children { get; set; }
    }
}
