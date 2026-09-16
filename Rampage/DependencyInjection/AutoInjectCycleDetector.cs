using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 启动期检测 [Auto] 立即解析依赖环（不含 Lazy&lt;T&gt; 边）。
    /// </summary>
    public static class AutoInjectCycleDetector
    {
        /// <summary>
        /// 发现环则抛 <see cref="InvalidOperationException"/>。
        /// </summary>
        public static void ValidateOrThrow(IEnumerable<Type> serviceImplementationTypes)
        {
            List<Type> targets = serviceImplementationTypes
                .Where(t => t != null && !t.IsAbstract && !t.IsInterface)
                .Distinct()
                .ToList()!;

            Dictionary<Type, HashSet<Type>> graph = BuildGraph(targets);
            List<Type>? cycle = FindCycle(graph);
            if (cycle == null || cycle.Count == 0)
            {
                return;
            }

            string path = string.Join(" -> ", cycle.Select(t => t.Name));
            throw new InvalidOperationException(
                "Circular AutoInject dependency detected (immediate [Auto]/[Singleton] resolve). " +
                "Break the cycle (one-way dependency / extract shared service) or use Lazy<T> for deferred inject. " +
                $"Cycle: {path}");
        }

        internal static Dictionary<Type, HashSet<Type>> BuildGraph(IReadOnlyList<Type> targets)
        {
            Dictionary<Type, HashSet<Type>> graph = new();
            foreach (Type host in targets)
            {
                if (!graph.ContainsKey(host))
                {
                    graph[host] = new HashSet<Type>();
                }

                AutoInjectionSchema? schema = AutoInjectionSchemaStore.GetSchema(host);
                if (schema?.AutoList == null)
                {
                    continue;
                }

                foreach (Auto auto in schema.AutoList)
                {
                    Type? memberType = auto.Property?.PropertyType ?? auto.Field?.FieldType;
                    if (memberType == null)
                    {
                        continue;
                    }

                    if (TryUnwrapLazy(memberType, out _))
                    {
                        // Lazy<T> 不在构造期解析，不构成立即环
                        continue;
                    }

                    foreach (Type impl in ResolveImplementations(memberType, targets))
                    {
                        if (impl != host)
                        {
                            graph[host].Add(impl);
                        }
                        else
                        {
                            // 自依赖也算环（立即解析自身）
                            graph[host].Add(impl);
                        }
                    }
                }
            }

            return graph;
        }

        internal static List<Type>? FindCycle(Dictionary<Type, HashSet<Type>> graph)
        {
            HashSet<Type> visiting = new();
            HashSet<Type> visited = new();
            Stack<Type> stack = new();

            foreach (Type node in graph.Keys)
            {
                List<Type>? cycle = Dfs(node, graph, visiting, visited, stack);
                if (cycle != null)
                {
                    return cycle;
                }
            }

            return null;
        }

        private static List<Type>? Dfs(
            Type node,
            Dictionary<Type, HashSet<Type>> graph,
            HashSet<Type> visiting,
            HashSet<Type> visited,
            Stack<Type> stack)
        {
            if (visited.Contains(node))
            {
                return null;
            }

            if (visiting.Contains(node))
            {
                List<Type> path = stack.Reverse().ToList();
                int start = path.IndexOf(node);
                if (start < 0)
                {
                    return new List<Type> { node, node };
                }

                List<Type> cycle = path.Skip(start).ToList();
                cycle.Add(node);
                return cycle;
            }

            visiting.Add(node);
            stack.Push(node);

            if (graph.TryGetValue(node, out HashSet<Type>? neighbors))
            {
                foreach (Type next in neighbors)
                {
                    List<Type>? cycle = Dfs(next, graph, visiting, visited, stack);
                    if (cycle != null)
                    {
                        return cycle;
                    }
                }
            }

            stack.Pop();
            visiting.Remove(node);
            visited.Add(node);
            return null;
        }

        public static bool TryUnwrapLazy(Type type, out Type elementType)
        {
            elementType = type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static List<Type> ResolveImplementations(Type serviceType, IReadOnlyList<Type> targets)
        {
            if (!serviceType.IsInterface && !serviceType.IsAbstract)
            {
                return new List<Type> { serviceType };
            }

            return targets.Where(t => serviceType.IsAssignableFrom(t)).ToList();
        }
    }
}
