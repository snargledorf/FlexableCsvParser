using System.Collections;

namespace CsvSpanParser
{
    internal sealed class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        private readonly Dictionary<char, TreeNode<T>> children = new();

        private readonly List<T> values = new();

        public TreeNode(char key, TreeNode<T>? parent = null)
        {
            Key = key;
            Parent = parent;
        }

        public char Key { get; }

        public TreeNode<T>? Parent { get; }

        public T[] Values => values.ToArray();

        public bool BranchIsWhiteSpace => char.IsWhiteSpace(Key) && (Parent?.BranchIsWhiteSpace ?? true);

        public TreeNode<T> Root => Parent?.Root ?? this;

        public bool TryGetChild(char c, out TreeNode<T>? node)
        {
            return children.TryGetValue(c, out node);
        }

        public void AddChild(char c, TreeNode<T> treeNode)
        {
            children.Add(c, treeNode);
        }

        public void AddValue(T value)
        {
            values.Add(value);
        }

        public override string ToString()
        {
            return Key.ToString();
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