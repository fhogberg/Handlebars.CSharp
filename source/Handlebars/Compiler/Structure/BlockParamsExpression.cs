using System;
using System.Linq.Expressions;

namespace HandlebarsDotNet.Compiler
{
    internal class BlockParamsExpression : HandlebarsExpression
    {
        public new static BlockParamsExpression Empty() => new BlockParamsExpression(null);

        private readonly BlockParam _blockParam;
        
        private BlockParamsExpression(BlockParam blockParam)
        {
            _blockParam = blockParam;
        }
        
        public BlockParamsExpression(string action, string blockParams)
            :this(new BlockParam
            {
                Action = action,
                Parameters = blockParams.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)
            })
        {
        }

        public override ExpressionType NodeType { get; } = (ExpressionType)HandlebarsExpressionType.BlockParamsExpression;

        public override Type Type { get; } = typeof(BlockParam);

        protected override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.Visit(Constant(_blockParam, typeof(BlockParam)));
        }
    }

    internal class BlockParam
    {
        public string Action { get; set; }
        public string[] Parameters { get; set; }
    }
}