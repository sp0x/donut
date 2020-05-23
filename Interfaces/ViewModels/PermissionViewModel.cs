using System;
using System.Collections.Generic;

namespace Donut.Interfaces.ViewModels
{
    public class PermissionViewModel
    {
        public long Id { get; set; }
        public OrganizationViewModel Owner { get; set; }
        public OrganizationViewModel ShareWith { get; set; }
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
    }
}