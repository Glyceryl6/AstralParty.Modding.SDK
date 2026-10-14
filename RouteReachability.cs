using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// A bounded, read-only board walk planner. This does not choose a branch for
/// another player, click a game arrow or claim the server will follow the path.
/// 有限深度的只读地块遍历；输出候选路径，不等同于服务器确定的行动路线。
/// </summary>
public static class RouteReachability {
    public const int DefaultMaxDepth = 12;
    public const int DefaultMaxRoutes = 24;
    public const int DefaultMaxBranches = 6;

    public static IReadOnlyList<ReachableRoute> Preview(int origin, int enteredFrom, int remainingSteps,
        Func<int, int, IReadOnlyList<int>> forwardNeighbors,
        int maxDepth = DefaultMaxDepth, int maxRoutes = DefaultMaxRoutes) {
        ArgumentNullException.ThrowIfNull(forwardNeighbors);
        if (origin < 0 || remainingSteps <= 0 || maxDepth <= 0 || maxRoutes <= 0) {
            return Array.Empty<ReachableRoute>();
        }
        int depth = Math.Min(remainingSteps, Math.Min(maxDepth, DefaultMaxDepth));
        int routesCap = Math.Min(maxRoutes, DefaultMaxRoutes);
        var routes = new List<ReachableRoute>();
        var path = new List<int> { origin };
        Explore(origin, enteredFrom, depth, forwardNeighbors, routesCap, routes, path);
        return routes;
    }

    private static void Explore(int current, int from, int depth,
        Func<int, int, IReadOnlyList<int>> forwardNeighbors, int limit,
        List<ReachableRoute> result, List<int> path) {
        if (result.Count >= limit) {
            return;
        }
        if (depth == 0) {
            result.Add(new ReachableRoute(path.ToArray()));
            return;
        }
        IReadOnlyList<int> next = forwardNeighbors(current, from);
        int visited = 0;
        for (int i = 0; i < next.Count && visited < DefaultMaxBranches && result.Count < limit; i++) {
            int id = next[i];
            if (id < 0 || id == from || id == current) {
                continue;
            }
            visited++;
            path.Add(id);
            Explore(id, current, depth - 1, forwardNeighbors, limit, result, path);
            path.RemoveAt(path.Count - 1);
        }
        if (visited == 0 && path.Count > 1) {
            result.Add(new ReachableRoute(path.ToArray()));
        }
    }
}

public sealed record ReachableRoute(IReadOnlyList<int> LandIds) {
    public int StepsShown => Math.Max(0, LandIds.Count - 1);
}
