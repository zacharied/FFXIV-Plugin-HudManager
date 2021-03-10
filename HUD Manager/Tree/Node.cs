using System;
using System.Collections.Generic;
using System.Linq;
using HUD_Manager.Configuration;

namespace HUD_Manager.Tree {
    public class Node<T> {
        public Guid Id { get; }
        public Node<T>? Parent { get; set; }
        public T Value { get; set; }
        public List<Node<T>> Children { get; } = new();

        public Node(Node<T>? parent, Guid id, T value) {
            this.Id = id;
            this.Parent = parent;
            this.Value = value;
        }

        private Node(Guid id) {
            this.Id = id;
            this.Value = default!;
        }

        public Node<T>? Find(Guid id) {
            if (this.Id == id) {
                return this;
            }

            foreach (var child in this.Children) {
                var result = child.Find(id);
                if (result != null) {
                    return result;
                }
            }

            return null;
        }

        public IEnumerable<Node<T>> Ancestors() {
            var parent = this.Parent;

            while (parent != null) {
                yield return parent;
                parent = parent.Parent;
            }
        }

        public IEnumerable<Node<T>> Traverse() {
            var stack = new Stack<Node<T>>();
            stack.Push(this);
            while (stack.Any()) {
                var next = stack.Pop();
                yield return next;
                foreach (var child in next.Children) {
                    stack.Push(child);
                }
            }
        }

        public IEnumerable<Tuple<Node<T>, uint>> TraverseWithDepth() {
            var stack = new Stack<Tuple<Node<T>, uint>>();
            stack.Push(Tuple.Create(this, (uint) 0));
            while (stack.Any()) {
                var next = stack.Pop();
                yield return next;
                foreach (var child in next.Item1.Children) {
                    stack.Push(Tuple.Create(child, next.Item2 + 1));
                }
            }
        }

        public static List<Node<SavedLayout>> BuildTree(Dictionary<Guid, SavedLayout> layouts) {
            var lookup = new Dictionary<Guid, Node<SavedLayout>>();
            var rootNodes = new List<Node<SavedLayout>>();

            foreach (var item in layouts) {
                if (lookup.TryGetValue(item.Key, out var ourNode)) {
                    ourNode.Value = item.Value;
                } else {
                    ourNode = new Node<SavedLayout>(null, item.Key, item.Value);
                    lookup[item.Key] = ourNode;
                }

                if (item.Value.Parent == Guid.Empty) {
                    rootNodes.Add(ourNode);
                } else {
                    if (!lookup.TryGetValue(item.Value.Parent, out var parentNode)) {
                        // create preliminary parent
                        parentNode = new Node<SavedLayout>(item.Value.Parent);
                        lookup[item.Value.Parent] = parentNode;
                    }

                    parentNode.Children.Add(ourNode);
                    ourNode.Parent = parentNode;
                }
            }

            return rootNodes;
        }
    }

    public static class NodeExt {
        public static Node<T>? Find<T>(this IEnumerable<Node<T>> nodes, Guid id) {
            foreach (var node in nodes) {
                var found = node.Find(id);

                if (found != null) {
                    return found;
                }
            }

            return null;
        }
    }
}
