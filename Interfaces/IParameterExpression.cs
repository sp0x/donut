using System.Collections.Generic;

namespace Donut.Interfaces
{
    public interface IParameterExpression : IExpression
    {
        IExpression Value { get; }
        string ToString();
    }
}