using System;
using System.Diagnostics;
using PEG.SyntaxTree;
using PEG.Utils;

namespace PEG
{
    /// <summary>
    /// Implements the memoization algorithm for grammar rules that forms the core of the PEG parser.
    /// The characteristics of the individual operators of the syntax are implemented in the classes in
    /// the syntax tree.  (Common.Core.Parsing.Peg.SyntaxTree)  The Nonterminal class enjoys a special
    /// relationship with this class, and directly invokes the ApplyNonterminal method to implement its
    /// behavior.  ParseEngine in turn calls back into the Nonterminal.Eval method, allowing that class
    /// to handle the bookkeeping of writing the begin and end markers to the output stream.
    /// </summary>
    public class ParseEngine
    {
        /// <summary>
        /// The actual string input.
        /// </summary>
        public string RawInput { get; set; }

        public ParseOutput Output { get; set; } = new ParseOutput();

        /// <summary>
        /// Current input symbol
        /// </summary>
        public Terminal Current => Position < Input.Length ? Input[Position] : null;

        /// <summary>
        /// The grammar
        /// </summary>
        public Grammar Grammar { get; set; }

        /// <summary>
        /// An implementation of IParseInput that could either represent a raw text input, or the
        /// result of a tokenization pass.
        /// </summary>
        public IParseInput Input { get; }

        /// <summary>
        /// The current position within Input.
        /// </summary>
        public int Position { get; protected set; }

        /// <summary>
        /// The recursion depth
        /// </summary>
        public int Depth { get; protected set; }

        /// <summary>
        /// Sometimes a Terminal can be created on the fly (for example, when using the AnyCharacter
        /// expression.)  This allows those terminals to still be cached and avoid recreating tons
        /// of terminals for the same character.
        /// </summary>
        public DynamicArray<Terminal> TerminalsCache { get; private set; }

        /// <summary>
        /// The heart of any PEG parser, this is the memo table that caches the results of every possible
        /// invocation of a nonterminal at a given position.  The first index is the nonterminal, and the
        /// second index is the position in the input.
        /// </summary>
        public MemoEntry?[,] MemoTable { get; set; }

        /// <summary>
        /// The farthest point the parser reached.  Usually this is a pretty good indicator of precisely
        /// where the parse failed.
        /// </summary>
        public int MaxPosition { get; set; }

        public bool IsLogEnabled { get; set; }

        public ParseOutputSpan False => new ParseOutputSpan(true, Position, Position);
        public ParseOutputSpan Nothing => new ParseOutputSpan(false, Position, Position);

        public ParseEngine(Grammar grammar, string input)
        {
            Grammar = grammar;
            Input = new StringParseInput(this, input);
            RawInput = input;
            Init();
        }

        public ParseEngine(Grammar grammar, string rawInput, IParseInput input)
        {
            Grammar = grammar;
            Input = input;
            RawInput = rawInput;
            Init();
        }

        private void Init()
        {
            TerminalsCache = new DynamicArray<Terminal>();
            MemoTable = new MemoEntry?[Input.Length + 1, Grammar.Nonterminals.Count];
            IsLogEnabled = true;
        }

        public const string Indent = "    ";

        [Conditional("LOG")]
        public void Log(string message)
        {
            if (!IsLogEnabled)
                return;

            for (int i = 0; i < Depth; i++)
                Console.Write(Indent);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Abstracts the idea of marking and resetting in case we ever need to keep track of
        /// more than the position.  (for now, merely returns the position)
        /// </summary>
        /// <returns></returns>
        public ParseMark Mark()
        {
            return new ParseMark(Position, Output.Count);
        }

        /// <summary>
        /// Increments the Position by 1.
        /// </summary>
        public bool Consume()
        {
            Log("Consumed " + Current.ToString());

            Position++;
            if (Position > MaxPosition)
                MaxPosition = Position;

            return true;
        }

        /// <summary>
        /// Restores the state of the parser to the previous mark.  (for now, merely restores the
        /// Position)
        /// </summary>
        /// <param name="mark"></param>
        public void Reset(ParseMark mark)
        {
            Position = mark.InputPosition;
            Output.Reset(mark.OutputPosition);
        }

        /// <summary>
        /// Invokes the nonterminal, and stores the result in the memo table.
        /// </summary>
        /// <param name="nonterminal"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual ParseOutputSpan ApplyNonterminal(Nonterminal nonterminal, int position)
        {
            Depth++;
            try
            {
                MemoEntry? memoEntry = MemoTable[position, nonterminal.Index];
                if (memoEntry == null)
                {
                    var answer = nonterminal.Eval(this);
                    memoEntry = new MemoEntry(answer, Position);
                    MemoTable[position, nonterminal.Index] = memoEntry;
                    return answer;
                }
                else
                {
                    Position = memoEntry.Value.Position;
                    return memoEntry.Value.Answer;
                }
            }
            finally
            {
                Depth--;
            }
        }

        public ParseOutputSpan Return(ParseMark mark)
        {
            return new ParseOutputSpan(false, mark.OutputPosition, Output.Count);
        }
    }
}