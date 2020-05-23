﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Donut.Data;
using Donut.Integration;
using Donut.Lex.Data;
using Donut.Lex.Expressions;
using Donut.Parsing.Tokens;
using Donut.Source;
using Donut.Interfaces;

namespace Donut.Lex.Parsing
{ 


    public class DonutSyntaxReader
    { 
        private List<string> _sourceIntegrations;

        private Donut.Data.DataIntegration[] _integrations;
        //private MatchCondition _currentMatchCondition;

        private const string ExpectedObjectErrorText = "Expected =, !=, IN or NOT IN but found: ";
        private OrderByExpression OrderBy { get; set; }
         
        private TokenReader Reader { get; set; }

        public DonutSyntaxReader()
        {
            _sourceIntegrations = new List<string>();
        }

        public DonutSyntaxReader(IEnumerable<DslToken> tokens, params Donut.Data.DataIntegration[] contextIntegrations)
            : this()
        {
            _integrations = contextIntegrations;
            if (_integrations == null) _integrations = new Donut.Data.DataIntegration[] { };
            Load(tokens);
        }

        /// <summary>
        /// Loads tokens for parsing
        /// </summary>
        /// <param name="tokens"></param>
        public void Load(IEnumerable<DslToken> tokens)
        {
            Reader = new TokenReader(tokens); 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DonutScript ParseDonutScript(IIntegration ign)
        {
            _sourceIntegrations.Clear();
            DonutScript newScript  = new DonutScript();
            Reader.DiscardToken(TokenType.Define);
            var newSymbolName = Reader.DiscardToken(TokenType.Symbol);
            newScript.Type = new ScriptTypeInfo(newSymbolName.Value);
            _sourceIntegrations = ReadFrom();
            var orderBy = ReadOrderBy(); 
            var expressions = ReadExpressions();
            newScript.StartingOrderBy = orderBy ;
            foreach (var expression in expressions)
            {
                var expressionType = expression.GetType();
                if (expressionType == typeof(AssignmentExpression))
                {
                    newScript.Features.Add(expression as AssignmentExpression);
                }
                else if (expressionType == typeof(TargetExpression))
                {
                    var targetExpr = expression as TargetExpression;
                    var targetFields = targetExpr.Attributes.Select(a => ign.Fields.FirstOrDefault(f=> f.Name==a));
                    newScript.Targets = new List<ModelTarget>();
                    foreach (var targetField in targetFields)
                    {
                        ((List<ModelTarget>)newScript.Targets).Add(new ModelTarget(targetField));
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            newScript.AddIntegrations(_sourceIntegrations.Select(i=> new Donut.Data.DataIntegration(i)).ToArray());

            return newScript;
        }

        /// <summary>
        /// Reads the next available expression.
        /// </summary>
        /// <returns></returns>
        public IExpression ReadExpression(Predicate<TokenMarker> terminatingPredicate = null)
        {
            return ReadExpressions(terminatingPredicate, 1).FirstOrDefault();
        }

        /// <summary>
        /// Reads the next available expressions
        /// </summary>
        /// <param name="terminatingPredicate">Reads untill this predicate is true</param>
        /// <param name="limit">Limit the number of records to read</param>
        /// <returns></returns>
        public IEnumerable<IExpression> ReadExpressions(Predicate<TokenMarker> terminatingPredicate = null, int limit = 0)
        {
            Stack<IExpression> previousExpressions = new Stack<IExpression>();
            //Queue<IExpression> outputExpressions = new Queue<IExpression>();
            uint ix = 0;
            while (!Reader.IsComplete)
            {
                if (terminatingPredicate!=null && terminatingPredicate(Reader.Marker)) break;
                if (limit != 0 && ix >= limit) break;

                var nextToken = Reader.Current;
                //IExpression crExpression = null;
                var lvlExpressions = new List<IExpression>();
//                if (!IsFirstCallExpression(Reader))
//                {
//                    nextToken = new SymbolToken(nextToken.Value);
//                }
                switch (nextToken.TokenType)
                {
                    case TokenType.Comma:                       //Skip commas
                        Reader.DiscardToken();
                        continue;
                    case TokenType.Reduce:
                        lvlExpressions.Add(ReadMapReduce());
                        break;
                    case TokenType.OrderBy:
                        lvlExpressions.Add(ReadOrderBy());
                        break;
                    case TokenType.Set:
                        var typeFeatureSet = ReadFeatureAssign();
                        lvlExpressions.Add(typeFeatureSet);
                        break;
                    case TokenType.Symbol:
                        if (IsFunctionCall(Reader.Current, Reader.NextToken))
                        {
                            var func = ReadFunctionCall();
                            lvlExpressions.Add(func);
                        }
                        else if (IsBadTargetDefinition(Reader.Current, Reader.NextToken))
                        {
                            throw new BadTarget("Target was unspecified or invalid.");
                        }
                        else if (IsVariableExpression(Reader.Current, Reader.NextToken))
                        {
                            var variable = ReadVariable();
                            lvlExpressions.Add(variable);
                        }
                        else
                        {
                            var member = ReadMemberChainExpression();
                            lvlExpressions.Add(member);
                        }
                        break;
                    case TokenType.Target:
                        var target = ReadTarget();
                        lvlExpressions.Add(target);
                        break;
                    case TokenType.NumberValue:
                        lvlExpressions.Add(ReadConstant());
                        break;
                    case TokenType.StringValue:
                        lvlExpressions.Add(ReadConstant());
                        break;
                    case TokenType.FloatValue:
                        lvlExpressions.Add(ReadConstant());
                        break;
                    case TokenType.Not:
                        var unaryOp = new UnaryExpression(nextToken);
                        Reader.DiscardToken();
                        unaryOp.Operand = ReadExpression();
                        lvlExpressions.Add(unaryOp);
                        break;
                    case TokenType.OpenParenthesis:
                        if (IsLambdaExpression(Reader))
                        {
                            var expLambda = ReadLambda(terminatingPredicate);
                            lvlExpressions.Add(expLambda);
                        }
                        else
                        {
                            var subExps = ReadParenthesisContent();
                            lvlExpressions.AddRange(subExps);
                        }
                        break;
                    case TokenType.Lambda:
                        var lambda = ReadLambda(terminatingPredicate);
                        lvlExpressions.Add(lambda);
                        break;
                    case TokenType.OpenBracket:
                        var arrExp = ReadArrayAccess(terminatingPredicate, nextToken, previousExpressions);
                        lvlExpressions.Add(arrExp);
                        break;
                    case TokenType.OpenCurlyBracket:
                        var blockExp = ReadBlockAccess();
                        lvlExpressions.Add(blockExp);
                        break;
                    case TokenType.NewLine:
                    case TokenType.Semicolon:
                        Reader.DiscardToken();
                        break;
                    case TokenType.MemberAccess: //Member access from the previous expression
                        var subMember = ReadMemberAccess(terminatingPredicate, nextToken, previousExpressions);
                        lvlExpressions.Add(subMember);
                        break;
                    case TokenType.First:
                        var fContent = ReadFirstExpressionContent();
                        lvlExpressions.Add(fContent);
                        break;
                    default:
                        if (IsOperator(nextToken))
                        {
                            var opExpression = ReadOperator(terminatingPredicate, nextToken, previousExpressions);//, outputExpressions);
                            lvlExpressions.Add(opExpression);
                        }
                        else
                        {
                            throw new Exception($"Unexpected token at {nextToken.Line}:{nextToken.Position}:\n " +
                                                $"{nextToken}");
                        }
                        break;
                }

                foreach (var lvlExpression in lvlExpressions)
                { 
                    previousExpressions.Push(lvlExpression);
                    Debug.WriteLine($"Read expression: {lvlExpression}");
                    ix++;
                } 
                //yield return crExpression; 
            }
            return previousExpressions.Reverse();
        }

        private CallExpression ReadFirstExpressionContent()
        {
            var fnExpression = new CallExpression();
            var tknFunctionName = Reader.DiscardToken(TokenType.First);
            Reader.DiscardToken(TokenType.OpenParenthesis);
            fnExpression.Name = "First";
            var cursor = Reader.Marker.Clone();
            //Create a predicate untill the closing of the function
            var isKeyword = Filters.Keyword(cursor);
            var isSameLevelComma = Filters.SameLevel(cursor, TokenType.Comma);
            var isSameLevelClosingBracket = Filters.SameLevel(cursor, TokenType.CloseBracket);
            var semiColonBreak = Filters.SameLevel(cursor, TokenType.Semicolon);
            var newLineBreak = Filters.SameLevel(cursor, TokenType.NewLine);
            var isExpressionBreak = new Predicate<TokenMarker>((x) =>
            {
                return semiColonBreak(x) || newLineBreak(x) || isSameLevelClosingBracket(x) || isKeyword(x) || isSameLevelComma(x);
            });

            while (!Reader.IsComplete && !isExpressionBreak(Reader.Marker))
            {
                ParameterExpression fnParameter = ReadFunctionParameter();
                fnExpression.AddParameter(fnParameter);
                if (Reader.Current.TokenType == TokenType.Comma)
                {
                    Reader.DiscardToken();
                }
            }
            return fnExpression;
        }

        private IExpression ReadBlockAccess()
        {
            Reader.DiscardToken(TokenType.OpenCurlyBracket);
            var expBlock = new BlockExpression();
            var marker = Reader.Marker.Clone();
            var blockEndPredicate = DonutSyntaxReader.Filters.BlockEnd(marker);
            var content = ReadExpressions(blockEndPredicate);
            Reader.DiscardToken(TokenType.CloseCurlyBracket);
            expBlock.Expressions = content.ToList();
            return expBlock;
        }

        private IExpression ReadArrayAccess(Predicate<TokenMarker> terminatingPredicate, DslToken token, Stack<IExpression> previousExpressions)
        { 
            var expArray = new ArrayAccessExpression();
            Reader.DiscardToken();
            var left = previousExpressions.Pop();
            expArray.Object = left; 
            var marker = Reader.Marker.Clone();
            var fnEndPredicate = DonutSyntaxReader.Filters.IndexArgumentsEnd(marker);
            while (!Reader.IsComplete && !fnEndPredicate(Reader.Marker))
            {
                ParameterExpression fnParameter = ReadIndexParameter();
                expArray.Parameters.Add(fnParameter);
                if (Reader.Current.TokenType == TokenType.Comma)
                {
                    Reader.DiscardToken();
                }
            }
            Reader.DiscardToken(TokenType.CloseBracket); 
            return expArray;
        }

        private IExpression ReadMemberAccess(
            Predicate<TokenMarker> terminatingPredicate,
            DslToken token, 
            Stack<IExpression> previousExpressions)
        { 
            Reader.DiscardToken();
            var left = previousExpressions.Pop();
            var member = new MemberExpression(left);

            //expArray.Object = left;
            var currentCursor = Reader.Marker.Clone();
            var isKeyword = Filters.Keyword(currentCursor);
            var isTerminating = Filters.ExpressionTermination(currentCursor);
            Predicate<TokenMarker> isComma = x => 
                x.Depth == currentCursor.Depth && x.Token.TokenType == TokenType.Comma;
            var valueExpression = ReadExpressions((c) =>
            {
                return isKeyword(c) || isTerminating(c) || isComma(c);
            }).FirstOrDefault();
            member.ChildMember = valueExpression; 
            return member
;
        }

        private IExpression ReadOperator(
            Predicate<TokenMarker> terminatingPredicate,
            DslToken token,
            Stack<IExpression> previousExpressions)
        {
            IExpression op = null;
            Reader.DiscardToken();
            var left = previousExpressions.Pop();  
            switch (token.TokenType)
            {
                case TokenType.Assign:
                    var right = ReadValueExpression(terminatingPredicate);
                    var aop = new AssignmentExpression(left as NameExpression, right);
                    op = aop;
                    break;
                default:
                    var bop = new BinaryExpression(token);
                    bop.Left = left;
                    bop.Right = ReadExpression(terminatingPredicate);
                    op = bop;
                    break;
            } 
            return op; 
        }

        private IExpression ReadConstant()
        {
            var token = Reader.DiscardToken();
            IExpression output = null;
            switch (token.TokenType)
            {
                case TokenType.NumberValue:
                    output = new NumberExpression() { Value = int.Parse(token.Value)};
                    break;
                case TokenType.StringValue:
                    output = new StringExpression() {Value = token.Value};
                    break;
                case TokenType.FloatValue:
                    output = new FloatExpression() {Value = float.Parse(token.Value)};
                    break;
                default:
                    throw new Exception("Unexpected token for constant!"); 
            }
            return output;
        }

        private bool IsOperator(DslToken token)
        {
            var tt = token.TokenType;
            return tt == TokenType.Add
                   || tt == TokenType.Subtract
                   || tt == TokenType.Multiply
                   || tt == TokenType.Divide
                   || tt == TokenType.Equals 
                   || tt == TokenType.NotEquals
                   || tt == TokenType.In
                   || tt == TokenType.NotIn
                   || tt == TokenType.Assign;
        }

        public NameExpression ReadVariable()
        {
            NameExpression exp = null; 
            //Read the symbol
            var token = Reader.DiscardToken(TokenType.Symbol);
            exp = new NameExpression(token.Value);
            if (Reader.Current.TokenType == TokenType.MemberAccess)
            {
                var memberChain = ReadMemberChainExpression();
                exp.Member = memberChain;
            }
            return exp;
        }

        public TargetExpression ReadTarget()
        {
            TargetExpression exp = null;
            var token = Reader.DiscardToken(TokenType.Target);
            exp = new TargetExpression(token.Value.Replace("target ", ""));
            return exp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public MemberExpression ReadMemberChainExpression()
        {
            if (Reader.Current.TokenType != TokenType.MemberAccess) return null;
            MemberExpression rootMember = null;
            MemberExpression previousMember = null;
            while (true)
            {
                if (Reader.Current.TokenType != TokenType.MemberAccess) break;
                var member = ReadMemberExpression();
                if (rootMember == null)
                {
                    rootMember = member; 
                }
                else
                {
                    previousMember.ChildMember = member;
                }
                previousMember = member;
            }
            return rootMember;
        }

        public MemberExpression ReadMemberExpression()
        {
            Reader.DiscardToken(TokenType.MemberAccess);
            if (IsFunctionCall(Reader.Current, Reader.NextToken))
            {
                var function = ReadFunctionCall();
                var fnMember = new MemberExpression(function);
                return fnMember;
            }
            else
            {
                var memberSymbol = Reader.DiscardToken(TokenType.Symbol);
                var variableExp = new NameExpression(memberSymbol.Value);
                MemberExpression member = new MemberExpression(variableExp);
                return member;
            }
            //ExpressionNode currentNode = null;
            //            while (true)
            //            {
            //                //if a dont is here, go a level deeper..
            //                var token = Reader.Current;
            //                
            //                if ((token.TokenType != TokenType.Symbol && token.TokenType != TokenType.MemberAccess)
            //                    || (cursorPredicate!=null && cursorPredicate(Reader.Cursor)))
            //                {
            //                    break;
            //                }
            //                switch (token.TokenType)
            //                {
            //                    case TokenType.MemberAccess:
            //                        Reader.DiscardToken();
            //                        break;
            //                    case TokenType.Symbol:
            //                        var newNode = new ExpressionNode(Reader.DiscardToken());
            //                        if (currentNode != null) currentNode.AddChild(newNode);
            //                        else
            //                        {
            //                            currentNode = newNode;
            //                        }
            //                        break;
            //                } 
            //                //read symbol
            // 
            //
            //            }
            //            return tree;
        }



        public AssignmentExpression ReadFeatureAssign()
        {
            Reader.DiscardToken(TokenType.Set);
            return ReadAssign();
        }

        public AssignmentExpression ReadAssign()
        {
            var featureVariable = ReadVariable();
//            if (FeatureModel.Type.Name != featureVariable.Name)
//            {
//                throw new Exception("Cannot assign feature to given type. The type is not defined.");
//            }
            Reader.DiscardToken(TokenType.Assign); 
            var value = ReadValueExpression();
            var setExp = new AssignmentExpression(featureVariable, value); 
            //FeatureModel.Features.Add(setExp);
            return setExp;
        }

        /// <summary>   Reads a value expression. </summary>
        ///
        /// <remarks>   Vasko, 05-Dec-17. </remarks>
        ///
        /// <exception cref="InvalidAssignedValue"> Thrown when an invalid assigned value error condition
        ///                                         occurs. </exception>
        ///
        /// <returns>   The value expression. </returns>

        public IExpression ReadValueExpression(Predicate<TokenMarker> terminatingPredicate = null)
        {
            var currentCursor = Reader.Marker.Clone();
            var isKeyword = Filters.Keyword(currentCursor);
            var isSameLevelComma = Filters.SameLevel(currentCursor, TokenType.Comma);
            var semiColonBreak = Filters.SameLevel(currentCursor, TokenType.Semicolon);
            var newLineBreak = Filters.SameLevel(currentCursor, TokenType.NewLine);
            var isExpressionBreak = new Predicate<TokenMarker>((x) =>
            {
                return semiColonBreak(x) || newLineBreak(x);
            });
            var valueExpressions = ReadExpressions((c) =>
            {
                return isKeyword(c) || isExpressionBreak(c) || isSameLevelComma(c) 
                || (terminatingPredicate != null && terminatingPredicate(c));
            });
            var value = valueExpressions.FirstOrDefault();
            if (valueExpressions.Count() > 1)
            {
                throw new InvalidAssignedValue(valueExpressions.ConcatExpressions());
            }
            return value;
        }

        /// <summary>   Reads a map reduce delcaration </summary>
        ///
        /// <remarks>   Vasko, 06-Dec-17. </remarks>
        ///
        /// <returns>   The map reduce expression. </returns>

        public MapReduceExpression ReadMapReduce()
        {
            if (Reader.Current.TokenType != TokenType.Reduce) return null;
            Reader.DiscardToken(TokenType.Reduce);
            var currentCursor = Reader.Marker.Clone();
            var reduceKeyExpressions = ReadExpressions(Filters.Keyword(currentCursor)); 
            Reader.DiscardToken(TokenType.ReduceMap);
            currentCursor = Reader.Marker.Clone();
            var valueExpressions = ReadExpressions(Filters.Keyword(currentCursor));
            IEnumerable<IExpression> aggregate = new List<IExpression>();
            if (Reader.Current.TokenType == TokenType.ReduceAggregate)
            {
                Reader.DiscardToken(TokenType.ReduceAggregate);
                currentCursor = Reader.Marker.Clone();
                aggregate = ReadExpressions(Filters.Keyword(currentCursor));
            }
            var mapReduce = new MapReduceExpression()
            {
                Keys = reduceKeyExpressions.Cast<AssignmentExpression>(),
                ValueMembers = valueExpressions.Cast<AssignmentExpression>(),
                Aggregate = new MapAggregateExpression(aggregate.Cast<AssignmentExpression>())
            };
            return mapReduce;
        }

        public OrderByExpression ReadOrderBy()
        {
            if (Reader.Current.TokenType != TokenType.OrderBy) return null;
            Reader.DiscardToken(TokenType.OrderBy);
            var currentCursor = Reader.Marker.Clone();
            var nextExpressions = ReadExpressions(Filters.Keyword(currentCursor));
            var orderBy = nextExpressions;
            var expression = this.OrderBy = new OrderByExpression(orderBy);
            return expression;
        } 

        /// <summary>   Reads the body of a lambda expression. </summary>
        ///
        /// <remarks>   Vasko, 25-Dec-17. </remarks>
        ///
        /// <returns>   The lambda. </returns>

        public LambdaExpression ReadLambda(Predicate<TokenMarker> predicate = null)
        {
            var parameters = new List<ParameterExpression>();
            var crToken = Reader.Current;
            TokenMarker marker = null;
            bool hasParenthesis = false;
            if (crToken.TokenType == TokenType.OpenParenthesis)
            {
                hasParenthesis = true;
                Reader.DiscardToken(TokenType.OpenParenthesis);
                marker = Reader.Marker.Clone();
                var fnEndPredicate = DonutSyntaxReader.Filters.FunctionCallEnd(marker);
                while (!Reader.IsComplete && !fnEndPredicate(Reader.Marker))
                {
                    ParameterExpression fnParameter = ReadFunctionParameter();
                    parameters.Add(fnParameter);
                    if (Reader.Current.TokenType == TokenType.Comma)
                    {
                        Reader.DiscardToken();
                    }
                }
            }
            if (hasParenthesis && Reader.Current.TokenType == TokenType.CloseParenthesis)
            {
                Reader.DiscardToken(TokenType.CloseParenthesis);
            }
            Reader.DiscardToken(TokenType.Lambda);
            var fBody = ReadExpressions(predicate);
            var lambda = new LambdaExpression(fBody);
            lambda.Parameters = parameters;
            return lambda;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CallExpression ReadFunctionCall()
        {
            var f = new CallExpression();
            var tknFunctionName = Reader.DiscardToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.OpenParenthesis);
            f.Name = tknFunctionName.Value;
            var cursor = Reader.Marker.Clone();
            //Create a predicate untill the closing of the function
            var fnEndPredicate = DonutSyntaxReader.Filters.FunctionCallEnd(cursor);
            while (!Reader.IsComplete && !fnEndPredicate(Reader.Marker))
            {
                ParameterExpression fnParameter = ReadFunctionParameter();
                f.AddParameter(fnParameter);
                if (Reader.Current.TokenType == TokenType.Comma)
                {
                    Reader.DiscardToken();
                }
            }
            Reader.DiscardToken(TokenType.CloseParenthesis);
            return f;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ParameterExpression ReadFunctionParameter()
        {
            var currentCursor = Reader.Marker.Clone();
            //Look for a comma on the same level
            var paramValue = ReadExpressions(DonutSyntaxReader.Filters.FunctionParameterEnd(currentCursor)).FirstOrDefault();
            if (paramValue == null) return null;
            var param = new ParameterExpression(paramValue); 
            return param;
        }

        public ParameterExpression ReadIndexParameter()
        {
            var currentCursor = Reader.Marker.Clone();
            //Look for a comma on the same level
            var paramValue = ReadExpressions(DonutSyntaxReader.Filters.IndexParameterEnd(currentCursor)).FirstOrDefault();
            if (paramValue == null) return null;
            var param = new ParameterExpression(paramValue);
            return param;
        }


        public IEnumerable<IExpression> ReadParenthesisContent()
        { 
            Reader.DiscardToken(TokenType.OpenParenthesis); 
            var startingCursor = Reader.Marker.Clone();
            //ExpressionNode currentNode = null;
            //Read untill closing parenthesis
            var subExpressions = ReadExpressions((c) =>
            {
                return c.Depth == startingCursor.Depth && c.Token.TokenType == TokenType.CloseParenthesis;
            })?.ToList();
            Reader.DiscardToken(TokenType.CloseParenthesis);
            return subExpressions;
        }

        /// <summary>
        /// Wether the two tokens form a function call
        /// </summary>
        /// <param name="tka"></param>
        /// <param name="tkb"></param>
        /// <returns></returns>
        private bool IsFunctionCall(DslToken tka, DslToken tkb)
        {
            return tka.TokenType == TokenType.Symbol 
                && tkb.TokenType == TokenType.OpenParenthesis;
        }

        /// <summary>
        /// Check if reader starts with a first_[DatasetName]_...
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private bool IsFirstCallExpression(TokenReader reader)
        {
            if (reader.Current.TokenType != TokenType.First) return false;
            var cursor = reader.Marker.Clone();
            if (this._integrations == null || this._integrations.Length == 0)
            {
                return false;
                //throw new InvalidDonutSyntax("Expression: first_ must be followed by a dataset");
            }
            var datasetName = reader.Marker.NextTokens.FirstOrDefault();
            return _integrations.Any(x => x.Name == datasetName.Value);
        }

        /// <summary>   Query if 'reader' starts with a lambda expression. </summary>
        ///
        /// <remarks>   Vasko, 26-Dec-17. </remarks>
        ///
        /// <param name="reader">  . </param>
        ///
        /// <returns>   True if reader starts with a lambda expression, false if not. </returns>

        private static bool IsLambdaExpression(TokenReader reader)
        {
            if (reader.Current.TokenType != TokenType.OpenParenthesis) return false;
            var cursor = reader.Marker.Clone();
            //Create a predicate untill the closing of the function 
            bool passedClosingParenthesis = false;
            var lambdaMarker = reader.SeekTo(x =>
            {
                if (x.Depth == cursor.Depth && x.Token.TokenType == TokenType.CloseParenthesis)
                {
                    passedClosingParenthesis = true;
                }
                if (x.Depth == cursor.Depth && passedClosingParenthesis
                && x.Token.TokenType == TokenType.Lambda) return true;
                return false;
            });
            return lambdaMarker != null;
        }

        
        public bool IsVariableExpression(DslToken tkA, DslToken tkB)
        {
            return tkA.TokenType == TokenType.Symbol
                   && (tkB.TokenType != TokenType.OpenParenthesis);
        }

        /// <summary>
        /// Checks if the 2 tokens are a bad target definition
        /// </summary>
        /// <param name="tkA"></param>
        /// <param name="tkB"></param>
        /// <returns></returns>
        public bool IsBadTargetDefinition(DslToken tkA, DslToken tkB)
        {
            return tkA.TokenType == TokenType.Symbol && tkA.Value == "target"
                                                     && (tkB.TokenType == TokenType.EOF);

        }


        private void ReadSymbolMemberAccess()
        {
            var symbol = Reader.ReadToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.Symbol);
            Reader.DiscardToken(TokenType.MemberAccess);
            var memberTokenStack = new Stack<DslToken>();
            while (true)
            {
                var memberToken = Reader.ReadToken(TokenType.Symbol);
                Reader.DiscardToken(TokenType.Symbol);
                memberTokenStack.Push(memberToken);
                if (Reader.Current.TokenType != TokenType.MemberAccess)
                {
                    break;
                }
            } 
        } 

        /// <summary>
        /// Reads from tokens
        /// </summary>
        private List<string> ReadFrom()
        {
            Reader.DiscardToken(TokenType.From);
            var fromCollections = new List<string>();
            do
            {
                var symbol = Reader.ReadToken(TokenType.Symbol);
                Reader.DiscardToken(TokenType.Symbol);
                fromCollections.Add(symbol.Value);
                if (Reader.Current.TokenType != TokenType.Comma) break;
            } while (true);
            return fromCollections;
        }

//        private void MatchCondition()
//        {
//            CreateNewMatchCondition();
//
//            if (IsObject(Reader.Current))
//            {
//                if (IsEqualityOperator(_lookaheadSecond))
//                {
//                    EqualityMatchCondition();
//                }
//                else if (_lookaheadSecond.TokenType == TokenType.In)
//                {
//                    InCondition();
//                }
//                else if (_lookaheadSecond.TokenType == TokenType.NotIn)
//                {
//                    NotInCondition();
//                }
//                else
//                {
//                    throw new DslParserException(ExpectedObjectErrorText + " " + _lookaheadSecond.Value);
//                }
//
//                MatchConditionNext();
//            }
//            else
//            {
//                throw new DslParserException(ExpectedObjectErrorText + _lookaheadFirst.Value);
//            }
//        }

        private void EqualityMatchCondition()
        {
            //_currentMatchCondition.Object = Reader.GetObject();
            Reader.DiscardToken();
            //_currentMatchCondition.Operator = Reader.GetOperator();
            Reader.DiscardToken();
            //_currentMatchCondition.Value = Reader.Current.Value;
            Reader.DiscardToken();
        }

        
        private void NotInCondition()
        {
            ParseInCondition(DslOperator.NotIn);
        }

        private void InCondition()
        {
            ParseInCondition(DslOperator.In);
        }

        private void ParseInCondition(DslOperator inOperator)
        {
            //_currentMatchCondition.Operator = inOperator;
            //_currentMatchCondition.Values = new List<string>();
            //_currentMatchCondition.Object = Reader.GetObject();
            Reader.DiscardToken();

            if (inOperator == DslOperator.In)
                Reader.DiscardToken(TokenType.In);
            else if (inOperator == DslOperator.NotIn)
                Reader.DiscardToken(TokenType.NotIn);

            Reader.DiscardToken(TokenType.OpenParenthesis);
            StringLiteralList();
            Reader.DiscardToken(TokenType.CloseParenthesis);
        }

        private void StringLiteralList()
        {
            //_currentMatchCondition.Values.Add(Reader.ReadToken(TokenType.StringValue).Value);
            Reader.DiscardToken(TokenType.StringValue);
            StringLiteralListNext();
        }

        private void StringLiteralListNext()
        {
            if (Reader.Current.TokenType == TokenType.Comma)
            {
                Reader.DiscardToken(TokenType.Comma);
                //_currentMatchCondition.Values.Add(Reader.ReadToken(TokenType.StringValue).Value);
                Reader.DiscardToken(TokenType.StringValue);
                StringLiteralListNext();
            }
            else
            {
                // nothing
            }
        }
         

        private bool IsNewExpressionStart(DslToken token)
        {
            switch (token.TokenType)
            {
                case TokenType.OpenParenthesis:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsObject(DslToken token)
        {
            return token.TokenType == TokenType.Collection
                   || token.TokenType == TokenType.Feature
                   || token.TokenType == TokenType.Type;
        } 

        private bool IsEqualityOperator(DslToken token)
        {
            return token.TokenType == TokenType.Equals
                   || token.TokenType == TokenType.NotEquals;
        }

//    
//
//        private void CreateNewMatchCondition()
//        {
//            _currentMatchCondition = new MatchCondition();
//            _featureModel.Filters.Add(_currentMatchCondition);
//        }


#region Helper predicates

        public static class Filters
        {
            public static bool IsKeyword(DslToken token)
            {
                return token!=null 
                    && token.TokenType == TokenType.OrderBy
                       || token.TokenType == TokenType.Set
                       || token.TokenType == TokenType.Reduce
                       || token.TokenType == TokenType.ReduceMap
                       || token.TokenType == TokenType.ReduceAggregate
                        || token.TokenType == TokenType.Target;
            }
            public static Predicate<TokenMarker> Keyword(TokenMarker currentMarker)
            {
                return x =>
                {
                    return IsKeyword(x.Token);
                };
            }
            
            /// <summary>   Returns a predicate for a cursor with a token on the same level as the given one. </summary>
            ///
            /// <remarks>   Vasko, 05-Dec-17. </remarks>
            ///
            /// <param name="currentMarker">    The current cursor. </param>
            /// <param name="tokenType">        Type of the token. </param>
            ///
            /// <returns>   A Predicate&lt;TokenCursor&gt; </returns>

            public static Predicate<TokenMarker> SameLevel(TokenMarker currentMarker, TokenType tokenType)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && x.Token != null
                           && x.Token.TokenType == tokenType;
                };
            }

            public static Predicate<TokenMarker> ExpressionTermination(TokenMarker currentMarker)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && (x.Token.TokenType == TokenType.Semicolon
                               || x.Token.TokenType == TokenType.NewLine);
                };
            }
            /// <summary>   Function call end predicate, checking for a closing parenthesis on current depth.. </summary>
            ///
            /// <remarks>   Vasko, 25-Dec-17. </remarks>
            ///
            /// <param name="currentMarker">    The current cursor. </param>
            ///
            /// <returns>   A Predicate&lt;TokenCursor&gt; </returns>

            public static Predicate<TokenMarker> FunctionCallEnd(TokenMarker currentMarker)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && x.Token != null 
                           && (x.Token.TokenType == TokenType.CloseParenthesis);
                };
            }

            public static Predicate<TokenMarker> IndexArgumentsEnd(TokenMarker currentMarker)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && x.Token != null
                           && (x.Token.TokenType == TokenType.CloseBracket);
                };
            }

            public static Predicate<TokenMarker> FunctionParameterEnd(TokenMarker currentMarker)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && x.Token != null 
                           && (x.Token.TokenType == TokenType.Comma || x.Token.TokenType == TokenType.CloseParenthesis);

                };
            }

            public static Predicate<TokenMarker> IndexParameterEnd(TokenMarker currentMarker)
            {
                return x =>
                {
                    return x.Depth == currentMarker.Depth
                           && x.Token != null
                           && (x.Token.TokenType == TokenType.Comma || x.Token.TokenType == TokenType.CloseBracket);

                };
            }

            public static Predicate<TokenMarker> BlockEnd(TokenMarker marker)
            {
                return x =>
                {
                    return x.Depth == marker.Depth
                           && x.Token != null
                           && (x.Token.TokenType == TokenType.CloseCurlyBracket);
                };
            }
        }
        
#endregion
    }
}
