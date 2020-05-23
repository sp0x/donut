using System.Collections.Generic;
using Donut.Interfaces;
using Donut.Interfaces.Models;

namespace Donut.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Rule
    {
        public long Id { get; set; }
        public string RuleName { get; set; }
        public string Type { get; set; }
        public virtual ICollection<Donut.Models.ModelRule> Models { get; set; }
        public virtual User Owner { get; set; }
        public bool IsActive { get; set; }

    }
}