using Plaszczakowo.Drawer.GraphDrawer;

namespace Plaszczakowo.ProblemVisualizer.Commands;

public class RemoveEdgeCommand(int edgeIndex)
    : ProblemVisualizerCommand<GraphData>
{
    private readonly int EdgeIndex = edgeIndex;

    public override void Execute(ref GraphData data)
    {
        if (EdgeIndex > data.Edges.Count || EdgeIndex < 0) return;
        data.RemoveEdge(EdgeIndex);
    }
}