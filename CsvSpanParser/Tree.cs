using System.Collections;

namespace CsvSpanParser
{
    internal sealed class Tree<T> : IEnumerable<TreeNode<T>>
    {
        private readonly Dictionary<char, TreeNode<T>> children = new();

        public Tree(params KeyValuePair<string, T>[] keyValues)
        {
            GenerateTreeNodes(keyValues);
        }

        private void GenerateTreeNodes(KeyValuePair<string, T>[] keyValues)
        {
            foreach (var kv in keyValues)
            {
                TreeNode<T>? currentNode = null;
                foreach (char nodeKey in kv.Key)
                {
                    if (currentNode is null)
                    {
                        if (!TryGetChild(nodeKey, out currentNode))
                            AddChild(nodeKey, currentNode = new TreeNode<T>(nodeKey));
                    }
                    else
                    {
                        if (currentNode.TryGetChild(nodeKey, out TreeNode<T>? nextNode))
                        {
                            currentNode = nextNode!;
                        }
                        else
                        {
                            currentNode.AddChild(nodeKey, currentNode = new TreeNode<T>(nodeKey, currentNode));
                        }
                    }
                }

                // This is the final node in this branch, add the value
                if (currentNode != null)
                    currentNode.Value = kv.Value;
            }
        }

        private bool TryGetChild(char c, out TreeNode<T>? node)
        {
            return children.TryGetValue(c, out node);
        }

        private void AddChild(char c, TreeNode<T> treeNode)
        {
            children.Add(c, treeNode);
        }

        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            return children.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}