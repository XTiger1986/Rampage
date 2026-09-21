using System;
using System.Collections.Generic;
using System.Linq;

namespace Rampage.DependencyInjection
{
    /// <summary>
    /// 启动期依赖环检测器：检测 [Auto] 立即解析形成的依赖环 / Startup dependency-cycle detector for eager [Auto] resolution.
    /// </summary>
    /// <remarks>
    /// 只检查立即解析边；<c>Lazy&lt;T&gt;</c> 边不在构造期解析，不计入环。
    /// Only eager-resolution edges are checked; <c>Lazy&lt;T&gt;</c> edges are not resolved at construction time and do not count as cycles.
    /// </remarks>
    public static class AutoInjectCycleDetector
    {
        /// <summary>
        /// 校验服务实现类型集合，发现环则抛出异常 / Validates the service implementation types and throws if a cycle is found.
        /// </summary>
        /// <param name="serviceImplementationTypes">待注册的服务实现类型集合 / The service implementation types to validate.</param>
        /// <exception cref="InvalidOperationException">检测到立即解析依赖环 / An eager-resolution dependency cycle was detected.</exception>
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

        /// <summary>
        /// 依据各类型的 [Auto] 成员构建依赖图 / Builds the dependency graph from the [Auto] members of each type.
        /// </summary>
        /// <param name="targets">候选实现类型列表 / The candidate implementation types.</param>
        /// <returns>宿主类型到其立即依赖实现类型的邻接表 / Adjacency list from host type to its eagerly-resolved dependency implementations.</returns>
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
                        // Lazy<T> 不在构造期解析，不构成立即环 / Lazy<T> is not resolved at construction time, so it does not form an eager cycle
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
                            // 自依赖也算环（立即解析自身） / Self-dependency also counts as a cycle (eagerly resolving itself)
                            graph[host].Add(impl);
                        }
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// 在依赖图中查找环 / Finds a cycle in the dependency graph.
        /// </summary>
        /// <param name="graph">邻接表 / The adjacency list.</param>
        /// <returns>成环的类型序列（首尾相同），无环返回 null / The cyclic type sequence (start equals end), or null when acyclic.</returns>
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

        /// <summary>三色标记 DFS：visiting 命中即回溯出环路径 / Three-color DFS: hitting a visiting node yields the cycle path by backtracking the stack.</summary>
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

        /// <summary>
        /// 判断类型是否为 Lazy&lt;T&gt; 并取出元素类型 / Determines whether the type is Lazy&lt;T&gt; and extracts its element type.
        /// </summary>
        /// <param name="type">待检查类型 / The type to check.</param>
        /// <param name="elementType">Lazy 的元素类型；非 Lazy 时原样返回 / The element type of the Lazy; returned unchanged when not a Lazy.</param>
        /// <returns>是 Lazy&lt;T&gt; 返回 true / True when the type is Lazy&lt;T&gt;.</returns>
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

        /// <summary>把成员类型解析为候选实现列表：具体类即自身，接口/抽象类则取可赋值的实现 / Resolves a member type to candidate implementations: itself when concrete, otherwise all assignable implementations.</summary>
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
