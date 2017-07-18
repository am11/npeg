using System.Collections.Generic;
using PEG.SyntaxTree;

namespace PEG.Cst
{
    public static class CstBuilder
    {
        public static CstNonterminalNode Build(ParseOutput parseOutput, ParseOutputSpan outputStream)
        {
            if (outputStream.IsFailed || !outputStream.Any)
                return null;

            Stack<CstNonterminalNode> stack = new Stack<CstNonterminalNode>();

            int absoluteIndex = 0;
            CstNonterminalNode current = new CstNonterminalNode(null, absoluteIndex);

            foreach (OutputRecord output in outputStream.GetRecords(parseOutput))
            {
                switch (output.OutputType)
                {
                    case OutputType.None:
                        current.Children.Add((Terminal)output.Expression);
                        break;
                    case OutputType.Begin:
                        CstNonterminalNode nonterminalNode = new CstNonterminalNode((Nonterminal)output.Expression, ++absoluteIndex);
                        stack.Push(current);
                        current = nonterminalNode;
                        break;
                    case OutputType.End:
                        CstNonterminalNode node = current;
                        current = stack.Pop();
                        current.Children.Add(node);
                        break;
                }
            }

            return current;
        }
    }
}