using CsvSpanParser.StateMachine;

namespace CsvSpanParser
{
    internal static class TokenizerStateMachineFactory
    {
        internal static StateMachine<int, char> CreateTokenizerStateMachine(TokenizerConfig config)
        {
            Tree<int> tree = CreateDelimiterConfigTree(config);

            /* This is the Goal
             * 
             * The start builder is done I think
            builder.From(FlexableTokenizerTokenState.Start)
                .When(',', FlexableTokenizerTokenState.EndOfFieldDelimiter)
                .When('\r', FlexableTokenizerTokenState.StartOfEndOfRecord)
                .When('"', FlexableTokenizerTokenState.EndOfFieldDelimiter)
                .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace)
                .Default(FlexableTokenizerTokenState.Text);

             * This needs more testing, should be built when building start state
            builder.From(FlexableTokenizerTokenState.StartOfEndOfRecord)
                .When('\n', FlexableTokenizerTokenState.EndOfEndOfRecord)
                .When('\r', FlexableTokenizerTokenState.EndOfWhiteSpace)
                .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace);

             * Currently not possible. Need to work around currently, but may be able to improve.
            builder.From(FlexableTokenizerTokenState.WhiteSpace)
                .When((c) => c == '\r' || !char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace);

            builder.From(FlexableTokenizerTokenState.Text)
                .When((c) => c == ',' || c == '"' || char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText);
            */
            return new StateMachine<int, char>(builder =>
            {
                BuildStartState(builder, tree);
                BuildWhiteSpaceState(builder, tree);
                BuildTextState(builder, tree);
            });
        }


        // This method builds the start state and any child states
        //builder.From(FlexableTokenizerTokenState.Start)
        //    .When(',', FlexableTokenizerTokenState.EndOfFieldDelimiter)
        //    .When('\r', FlexableTokenizerTokenState.StartOfEndOfRecord)
        //    .When('"', FlexableTokenizerTokenState.EndOfFieldDelimiter)
        //    .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace)
        //    .Default(FlexableTokenizerTokenState.Text);

        // * This needs more testing, should be built when building start state
        //builder.From(FlexableTokenizerTokenState.StartOfEndOfRecord)
        //    .When('\n', FlexableTokenizerTokenState.EndOfEndOfRecord)
        //    .When('\r', FlexableTokenizerTokenState.EndOfWhiteSpace)
        //    .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace);
        private static void BuildStartState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree)
        {
            ITransitionMapBuilder<int, char> startBuilder = builder.From(FlexableTokenizerTokenState.Start);
            int stateId = FlexableTokenizerTokenState.StartOfAdditionalStates;
            foreach (var node in tree)
            {
                BuildTransitions(node, startBuilder, ref stateId);
            }

            startBuilder
                .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace)
                .Default(FlexableTokenizerTokenState.Text);
        }

        // Wants to build this -
        // .When((c) => c == '\r' || !char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace);
        //
        // Actually builds this -
        // .When(char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText)
        // .When(',', FlexableTokenizerTokenState.EndOfText)
        // .When('"', FlexableTokenizerTokenState.EndOfText)
        private static void BuildWhiteSpaceState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree)
        {
            ITransitionMapBuilder<int, char> whiteSpaceBuilder = builder.From(FlexableTokenizerTokenState.WhiteSpace).When(c => !char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace);

            // Workaround
            // TODO Figure out how to create an OR expression since it's probably faster
            // Ex. .When((c) => c == '\r' || !char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace);
            foreach (var node in tree)
            {
                if (node.BranchIsWhiteSpace)
                    whiteSpaceBuilder.When(node.Key, FlexableTokenizerTokenState.EndOfWhiteSpace);
            }
        }

        // Wants to build this -
        // .When((c) => c == ',' || c == '"' || char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText);
        //
        // Actually builds this -
        // .When(char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText)
        // .When(',', FlexableTokenizerTokenState.EndOfText)
        // .When('"', FlexableTokenizerTokenState.EndOfText)
        private static void BuildTextState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree)
        {
            ITransitionMapBuilder<int, char> textBuilder = builder.From(FlexableTokenizerTokenState.Text).When(c => char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText);

            // Workaround
            // TODO Figure out how to create an OR expression since it's probably faster
            // Ex. .When((c) => c == '"' || char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText);
            foreach (var node in tree)
            {
                if (!node.BranchIsWhiteSpace)
                    textBuilder.When(node.Key, FlexableTokenizerTokenState.EndOfText);
            }
        }

        private static void BuildTransitions(TreeNode<int> node, ITransitionMapBuilder<int, char> currentMapBuilder, ref int stateId)
        {
            // If this node has a value then it should be treated as a final node
            // This does break instances where a delimiter may be the start of another delimiter
            // TODO Figure out how to hold onto a "Potential state" if a larger state doesn't workout (Substates?)
            if (node.Values.Length > 0)
            {
                currentMapBuilder.When(node.Key, node.Values[0]);

                // If this whole branch is whitespace then add checks for WhiteSpace situations
                if (node.BranchIsWhiteSpace)
                {
                    TreeNode<int> rootNode = node.Root;
                    // We might be starting another one of these branches \r\r\n = \r {whitespace} \r\n {record}
                    currentMapBuilder.When(rootNode.Key, FlexableTokenizerTokenState.EndOfWhiteSpace);

                    // Or there could be some other type of whitespace character \r\t = {whitespace}
                    currentMapBuilder.When(c => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace);
                }
            }
            else
            {
                // We have children, so this is just an intermediate node
                currentMapBuilder.When(node.Key, stateId);

                currentMapBuilder = currentMapBuilder.StateMachineTransitionMapBuilder.From(stateId);

                foreach (var childNode in node)
                {
                    stateId++;
                    BuildTransitions(childNode, currentMapBuilder, ref stateId);
                }
            }
        }

        private static Tree<int> CreateDelimiterConfigTree(TokenizerConfig config)
        {
            var delimitersToStates = new[]
                            {
            new KeyValuePair<string, int>(config.FieldDelimiter, FlexableTokenizerTokenState.EndOfFieldDelimiter),
            new KeyValuePair<string, int>(config.RecordDelimiter, FlexableTokenizerTokenState.EndOfEndOfRecord),
            new KeyValuePair<string, int>(config.QuoteDelimiter, FlexableTokenizerTokenState.EndOfQuote),
            new KeyValuePair<string, int>(config.EscapeDelimiter, FlexableTokenizerTokenState.EndOfEscape),
        };

            return new Tree<int>(delimitersToStates);
        }
    }
}
