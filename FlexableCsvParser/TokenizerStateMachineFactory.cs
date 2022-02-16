using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using FastState;

namespace FlexableCsvParser
{
    internal static class TokenizerStateMachineFactory
    {
        internal static StateMachine<int, char> CreateTokenizerStateMachine(Delimiters config)
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
            IStateTransitionMapBuilder<int, char> startBuilder = builder.From(FlexableTokenizerTokenState.Start);
            
            int stateId = FlexableTokenizerTokenState.StartOfAdditionalStates;
            foreach (var node in tree)
                BuildTransitions(node, startBuilder, ref stateId);

            startBuilder
                .When((c) => char.IsWhiteSpace(c), FlexableTokenizerTokenState.WhiteSpace)
                .Default(FlexableTokenizerTokenState.Text);
        }


        // Builds this -
        // .When('\r', FlexableTokenizerTokenState.EndOfWhiteSpace)
        // .When(!char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace)
        private static void BuildWhiteSpaceState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree)
        {
            BuildTextOrWhiteSpaceState(builder, tree, true);
        }

        // Builds this -
        // .When(',', FlexableTokenizerTokenState.EndOfText)
        // .When('"', FlexableTokenizerTokenState.EndOfText)
        // .When(char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText)
        private static void BuildTextState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree)
        {
            BuildTextOrWhiteSpaceState(builder, tree, false);
        }


        // Builds this -
        // .When(',', FlexableTokenizerTokenState.EndOfText)
        // .When('"', FlexableTokenizerTokenState.EndOfText)
        // .When(char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfText)
        // Or this -
        // .When('\r', FlexableTokenizerTokenState.EndOfText)
        // .When(!char.IsWhiteSpace(c), FlexableTokenizerTokenState.EndOfWhiteSpace)
        private static void BuildTextOrWhiteSpaceState(IStateMachineTransitionMapBuilder<int, char> builder, Tree<int> tree, bool whiteSpace)
        {
            var currentState = whiteSpace ? FlexableTokenizerTokenState.WhiteSpace : FlexableTokenizerTokenState.Text;
            var nextState = whiteSpace ? FlexableTokenizerTokenState.EndOfWhiteSpace : FlexableTokenizerTokenState.EndOfText;

            IStateTransitionMapBuilder<int, char> textBuilder = builder.From(currentState);

            foreach (var node in tree)
            {
                if (whiteSpace)
                {
                    if (node.BranchIsWhiteSpace)
                        textBuilder.When(node.Key, nextState);
                }
                else if (!node.BranchIsWhiteSpace)
                    textBuilder.When(node.Key, nextState);
            }

            Expression<Func<char, bool>> isWhiteSpaceExpresion = whiteSpace
                ? GetExpression((c) => !char.IsWhiteSpace(c))
                : GetExpression((c) => char.IsWhiteSpace(c));

            textBuilder.When(isWhiteSpaceExpresion, nextState);

            static Expression<Func<char, bool>> GetExpression(Expression<Func<char, bool>> expression)
            {
                return expression;
            }
        }

        private static void BuildTransitions(TreeNode<int> node, IStateTransitionMapBuilder<int, char> currentMapBuilder, ref int stateId)
        {
            // If this node has a value then it should be treated as a final node
            // This breaks instances where a control string may be the start of another control string
            // Ex. " vs. "", " will be caught but not ""
            // Ex 2. <Foo vs. <FooBar, <FooBar will never be hit since <Foo finished first
            // Need to build states to check for longer control strings
            if (node.HasValue)
            {
                if (node.HasChildren)
                {
                    // Build follow-up states to check for longer control strings
                    // Ex. " vs. ""
                    //
                    // We already matched on ", so now we need a state to possibly fall back to " if "" doesn't work out
                    //
                    // builder.From([State id for "])
                    //  .When('"', [Escape child node].Value) // IE. FlexableTokenizerTokenState.EndOfEscape
                    //  .Default(node.Value);
                    //
                    //-------------------------------------------------------------------------------
                    // Ex 2. <Foo vs. <FooB vs. <FooBar
                    //
                    // We already matched on <Foo
                    // so now we need states to possibly fall back to <Foo if <FooB fails
                    // or <FooB if <FooBar fails
                    //
                    // currentMapBuilder.When('o', [State id for second o]);
                    //
                    // builder.From([State id for second o])
                    //  .When('B', [State id for B])
                    //  .Default(node.Value);
                    //
                    // builder.From([State id for second B])
                    //  .When('a', [State id for a])
                    //  .Default([<FooB node].Value);
                    //
                    // builder.From([State id for a])
                    //  .When('r', [<FooBar node].Value)
                    //  .Default([<FooB node].Value);
                    //
                    // etc.

                    currentMapBuilder.When(node.Key, stateId);
                    var subStateBuilder = currentMapBuilder.StateMachineTransitionMapBuilder.From(stateId++);

                    foreach (var childNode in node)
                    {
                        BuildTransitions(childNode, subStateBuilder, ref stateId);
                        subStateBuilder.Default(node.Value);
                    }
                }
                else
                {
                    // If this node had no children, then we need to switch to a dummy state
                    // to ensure the character is read
                    currentMapBuilder.When(node.Key, stateId);

                    // The dummy state just defaults to the final state from the node Value
                    currentMapBuilder.StateMachineTransitionMapBuilder.From(stateId++)
                        .Default(node.Value);

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
            }
            else
            {
                // If we don't have a value then this is just an intermediate node and must have children
                currentMapBuilder.When(node.Key, stateId);

                currentMapBuilder = currentMapBuilder.StateMachineTransitionMapBuilder.From(stateId);

                foreach (var childNode in node)
                {
                    stateId++;
                    BuildTransitions(childNode, currentMapBuilder, ref stateId);
                }
            }
        }

        private static Tree<int> CreateDelimiterConfigTree(Delimiters config)
        {
            var delimitersToStates = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>(config.Field, FlexableTokenizerTokenState.EndOfFieldDelimiter),
                new KeyValuePair<string, int>(config.EndOfRecord, FlexableTokenizerTokenState.EndOfEndOfRecord),
            };

            if (!string.IsNullOrEmpty(config.Quote))
                delimitersToStates.Add(new KeyValuePair<string, int>(config.Quote, FlexableTokenizerTokenState.EndOfQuote));
            if (!string.IsNullOrEmpty(config.Escape))
                delimitersToStates.Add(new KeyValuePair<string, int>(config.Escape, FlexableTokenizerTokenState.EndOfEscape));

            return new Tree<int>(delimitersToStates.ToArray());
        }
    }
}
