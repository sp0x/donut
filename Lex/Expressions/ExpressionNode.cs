using System.Collections.Generic;
using Donut.Parsing.Tokens;
using Netlyt.Interfaces;

namespace Donut.Lex.Expressions
{
    public class ExpressionNode
        : Expression
    {
        protected Stack<IExpression> Children { get; set; } 
        private DslToken Token { get; set; }
        public short Depth { get; private set; }

        public ExpressionNode()
        {
            Children = new Stack<IExpression>();
        }
        public ExpressionNode(DslToken token)
            : this()
        { 
            this.Token = token;
        }
        public void SetDepth(short d) { this.Depth = d; }

        public ExpressionNode AddChild(DslToken token)
        {
            var node = new ExpressionNode(token);
            node.Parent = this;
            Children.Push(node);
            return this;
        }
        public ExpressionNode AddChild(ExpressionNode node)
        { 
            node.Parent = this;
            Children.Push(node);
            return this;
        }

        //        public ExpressionNode AddChild(IExpression node)
        //        {
        //            Children.Push(node);
        //            return this;
        //        }
         

        public int GetChildrenCount()
        {
            return Children.Count;
        }

        public static ExpressionNode Wrap(IExpression memberAccessTree)
        {
            var root = new ExpressionNode();
            
            throw new System.NotImplementedException();
        }
    }
}