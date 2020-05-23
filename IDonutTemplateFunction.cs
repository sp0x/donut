using Donut.Lex.Expressions;
using Donut.Interfaces;

namespace Donut
{
    public interface IDonutTemplateFunction
    {

    }
    public interface IDonutTemplateFunction<T> : IDonutTemplateFunction, IDonutFunction
    {
        T GetTemplate(CallExpression exp, DonutCodeContext context);
    }
}