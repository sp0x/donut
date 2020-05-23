using System.Collections.Generic;
using Donut.Interfaces;

namespace Donut.Lex.Expressions
{
    public abstract class Expression 
        : IExpression
    { 
        public IExpression Parent { get; set; }
        protected Expression()
        { 
        }
        protected Expression(IExpression parent) 
            : this()
        {
            this.Parent = parent;
        }

        public virtual IEnumerable<IExpression> GetChildren()
        {
            //Just a stub;
            return new List<IExpression>();
        }
    }
}