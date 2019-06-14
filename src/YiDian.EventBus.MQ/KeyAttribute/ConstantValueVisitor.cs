using System;
using System.Linq.Expressions;
using System.Reflection;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    internal static class ConstantValueVisitor
    {
        public static object GetParameExpressionValue(this Expression expression)
        {
            bool isConstant;
            return GetParameExpressionValue(expression, out isConstant);
        }
        public static object GetParameExpressionValue(Expression expression, out bool isConstant)
        {
            isConstant = false;
            //只能处理常量
            if (expression is ConstantExpression)
            {
                isConstant = true;
                ConstantExpression cExp = (ConstantExpression)expression;
                return cExp.Value;
            }
            else if (expression is MemberExpression)//按属性访问
            {
                var m = expression as MemberExpression;
                if (m.Expression != null)
                {
                    if (m.Expression.NodeType == ExpressionType.Parameter)
                    {
                        return m.Member.Name;
                    }
                    else
                    {
                        return GetMemberExpressionValue(m, out isConstant);
                    }
                }
                return GetMemberExpressionValue(m, out isConstant);
            }
            //按编译
            return Expression.Lambda(expression).Compile().DynamicInvoke();
        }
        public static object GetMemberExpressionValue(Expression exp, out bool isConstant)
        {
            isConstant = false;
            switch (exp.NodeType)
            {
                case ExpressionType.Constant:
                    isConstant = true;
                    return ((ConstantExpression)exp).Value;
                case ExpressionType.MemberAccess:
                    var mExp = (MemberExpression)exp;
                    object instance = null;
                    if (mExp.Expression != null)
                    {
                        instance = GetMemberExpressionValue(mExp.Expression, out isConstant);
                        //字段属属性都按变量
                        isConstant = false;
                        if (instance == null)
                        {
                            throw new ArgumentNullException(exp.ToString());
                        }
                    }

                    if (mExp.Member.MemberType == MemberTypes.Field)
                    {
                        return ((FieldInfo)mExp.Member).GetValue(instance);
                    }
                    else if (mExp.Member.MemberType == MemberTypes.Property)
                    {
                        return ((PropertyInfo)mExp.Member).GetValue(instance, null);
                    }
                    throw new Exception("未能解析" + mExp.Member.MemberType);
                case ExpressionType.ArrayIndex:
                    var arryExp = exp as BinaryExpression;
                    var array = GetMemberExpressionValue(arryExp.Left, out isConstant) as Array;
                    var index = (int)((ConstantExpression)arryExp.Right).Value;
                    return array.GetValue(index);
            }
            throw new Exception("未能解析" + exp.NodeType);
        }
    }
}
