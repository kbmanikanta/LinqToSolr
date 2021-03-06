﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#if NET35
using LinqToSolr.Expressions;
#endif


namespace LinqToSolr.Expressions
{
    public static class Evaluator
    {
        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }


        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

#if NET35
        internal class SubtreeEvaluator: ExpressionVisitorNet35
#else
        internal class SubtreeEvaluator: ExpressionVisitor
#endif
        {

            HashSet<Expression> candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return Visit(exp);
            }

#if NET35
            protected override Expression Visit(Expression exp)
#else
            public override Expression Visit(Expression exp)
#endif
            {

                if (exp == null)
                {
                    return null;
                }

                if (candidates.Contains(exp))
                {
                    return Evaluate(exp);
                }

                return base.Visit(exp);
            }
            private Expression Evaluate(Expression e)
            {

                if (e.NodeType == ExpressionType.Constant)
                {

                    return e;

                }

                LambdaExpression lambda = Expression.Lambda(e);

                Delegate fn = lambda.Compile();

                return Expression.Constant(fn.DynamicInvoke(null), e.Type);

            }

        }



#if NET35
        class Nominator: ExpressionVisitorNet35
#else
        class Nominator: ExpressionVisitor

#endif
        {
            readonly Func<Expression, bool> _fnCanBeEvaluated;

            HashSet<Expression> _candidates;

            bool _cannotBeEvaluated;



            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {

                _fnCanBeEvaluated = fnCanBeEvaluated;

            }



            internal HashSet<Expression> Nominate(Expression expression)
            {

                _candidates = new HashSet<Expression>();

                Visit(expression);

                return _candidates;

            }


#if NET35
            protected override Expression Visit(Expression expression)
#else
            public override Expression Visit(Expression expression)
#endif
            {

                if (expression != null)
                {

                    bool saveCannotBeEvaluated = _cannotBeEvaluated;

                    _cannotBeEvaluated = false;

                    base.Visit(expression);

                    if (!_cannotBeEvaluated)
                    {

                        if (_fnCanBeEvaluated(expression))
                        {

                            _candidates.Add(expression);

                        }

                        else
                        {

                            _cannotBeEvaluated = true;

                        }

                    }

                    _cannotBeEvaluated |= saveCannotBeEvaluated;

                }

                return expression;

            }

        }

    }
}