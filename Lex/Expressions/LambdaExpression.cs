﻿using System.Collections.Generic;
using System.Text;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class LambdaExpression : Expression
    {
        public List<ParameterExpression> Parameters { get; set; }
        public IEnumerable<IExpression> Body { get; private set; }

        public LambdaExpression()
        {

        }
        public LambdaExpression(IEnumerable<IExpression> fBody)
        {
            Body = fBody;
        }
        
        public override IEnumerable<IExpression> GetChildren()
        {
            return new List<IExpression> {};
        }

        public override string ToString()
        {
            var paramBuilder = new StringBuilder();
            paramBuilder.Append("(");
            var paramsStr = Parameters.ConcatExpressions();
            paramBuilder.Append(paramsStr);
            paramBuilder.Append(") => ");
            var strBody = Body.ConcatExpressions();
            paramBuilder.Append(strBody);
            return paramBuilder.ToString();
        }
    }
}
