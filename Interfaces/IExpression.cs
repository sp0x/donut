using System.Collections.Generic;

namespace Donut.Interfaces
{
    public interface IExpression
    {
        IExpression Parent { get; set; } 
        IEnumerable<IExpression> GetChildren();
    }
}