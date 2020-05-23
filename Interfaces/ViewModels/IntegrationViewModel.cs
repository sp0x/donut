using System.Collections.Generic;

namespace Donut.Interfaces.ViewModels
{
    public class IntegrationViewModel
    {
        public IntegrationSchemaViewModel Schema { get; set; }
        public bool UserIsOwner { get; set; }
        public IEnumerable<AccessLogViewModel> AccessLog { get; set; }
        public IEnumerable<PermissionViewModel> Permissions { get; set; }
    }
}