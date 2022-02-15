using System;
using System.Collections;
using System.Collections.Generic;

namespace FlexableCsvParser
{
    internal sealed class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        private readonly Dictionary<char, TreeNode<T>> children = new Dictionary<char, TreeNode<T>>();
        private T value;

        public TreeNode(char key, TreeNode<T> parent = null)
        {
            Key = key;
            Parent = parent;
        }

        public char Key { get; }

        public TreeNode<T> Parent { get; }

        public T Value
        {
            get => value;
            set
            {
                if (HasValue)
                    throw new InvalidOperationException("TreeNode already has a value");

                this.value = value;
                HasValue = true;
            }
        }

        public bool HasChildren => children.Count > 0;

        public bool BranchIsWhiteSpace => char.IsWhiteSpace(Key) && (Parent?.BranchIsWhiteSpace ?? true);

        public TreeNode<T> Root => Parent?.Root ?? this;

        public bool TryGetChild(char c, out TreeNode<T> node)
        {
            return children.TryGetValue(c, out node);
        }

        public void AddChild(char c, TreeNode<T> treeNode)
        {
            children.Add(c, treeNode);
        }

        public bool HasValue { get; private set; }

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