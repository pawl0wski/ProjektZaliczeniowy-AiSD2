using Plaszczakowo.Drawer.GraphDrawer;
using Plaszczakowo.Drawer.GraphDrawer.Images;
using Plaszczakowo.ProblemResolver;
using Plaszczakowo.ProblemResolver.ProblemGraph;
using Plaszczakowo.Problems.FenceTransport.Input;
using Plaszczakowo.Problems.FenceTransport.Output;
using Plaszczakowo.ProblemVisualizer.Commands;

namespace Plaszczakowo.Problems.FenceTransport;

public class FenceTransportResolver : ProblemResolver<FenceTransportInputData, FenceTransportOutput, GraphData>
{
    private int _factoryIndex;
    private ProblemRecreationCommands<GraphData>? problemRecreationCommands;

    public override FenceTransportOutput Resolve(FenceTransportInputData data,
        ref ProblemRecreationCommands<GraphData> commands)
    {
        var ConvexHullEdgesIndexes = AddHullEdges(data.Vertices, data.Edges,
            data.ConvexHullOutput!.HullIndexes!);
        
        FenceTransportOutput output = new();
        SetFactoryVertex(data);
        problemRecreationCommands = commands;
        var hoursCount = 0;
        var carriers = CreateCarriers(data);
        if (carriers.Count == 0)
        {
            output.HoursToBuild = -1;
            return output;
        }

        data.Vertices[_factoryIndex].Value = carriers.Count;
        var firstVertex = GetUnfinishedVertieces(data).First();
        while (GetUnfinishedVertieces(data).Count > 0)
        {
            var moved = false;
            foreach (var carrier in carriers)
                switch (carrier.State)
                {
                    case CarrierState.Unassigned:
                        AssignCarrierVertexToBuild(carrier, data);
                        break;
                    case CarrierState.Delivering:
                        if (carrier.CurrentRoute.Count > 0)
                        {
                            MoveCarrierOnGraph(carrier);
                            moved = true;
                        }
                        else
                        {
                            var unfinishedEdges = GetUnfifinishedEdgesConnectedToVertex(carrier.Position, data);
                            if (unfinishedEdges.Count > 0)
                            {
                                var edgeToVisit = unfinishedEdges
                                    .OrderByDescending(e => e.Throughput?.Capacity - e.Throughput?.Flow)
                                    .FirstOrDefault()!;
                                carrier.CurrentRoute.Enqueue(GetSecondEndOfEdge(carrier.Position, edgeToVisit,
                                    data.Vertices)!);
                                carrier.EdgeToBuild = edgeToVisit;
                                carrier.State = CarrierState.Building;
                            }
                            else
                            {
                                carrier.State = CarrierState.Unassigned;
                                AssignCarrierVertexToBuild(carrier, data);
                            }
                        }

                        break;
                    case CarrierState.Building:
                        MoveCarrierOnGraph(carrier);
                        moved = true;
                        carrier.Deliver();
                        problemRecreationCommands.Add(new ChangeEdgeFlowCommand(carrier!.EdgeToBuild!.Id,
                            new GraphThroughput(carrier!.EdgeToBuild!.Throughput!.Flow,
                                carrier.EdgeToBuild.Throughput.Capacity)));

                        if (carrier!.Load > 0)
                            AssignCarrierVertexToBuild(carrier, data);
                        else
                            ReturnCarrierToFactory(carrier, data);
                        break;
                    case CarrierState.Reffiling:
                        if (carrier.CurrentRoute.Count > 0)
                        {
                            MoveCarrierOnGraph(carrier);
                            moved = true;
                        }
                        else
                        {
                            carrier.Refill();
                            AssignCarrierVertexToBuild(carrier, data);
                        }

                        break;
                }

            if (moved)
            {
                problemRecreationCommands.NextStep();
                hoursCount++;
            }
        }

        output.HoursToBuild = hoursCount;
        return output;
    }

    private void SetFactoryVertex(FenceTransportInputData data)
    {
        _factoryIndex = data.Vertices.First(x => x.IsSpecial).Id;
    }

    private List<Carrier> CreateCarriers(FenceTransportInputData data)
    {
        List<Carrier> carriers = [];
        for (var i = 0; i < (data.CarrierAssignmentOutput?.Pairs.Count ?? 0); i++)
            carriers.Add(new Carrier(i, data.Vertices[_factoryIndex]));
        return carriers;
    }

    private void MoveCarrierOnGraph(Carrier carrier)
    {
        carrier.Position.Value -= 1;
        problemRecreationCommands?.Add(new ChangeVertexValueCommand(carrier.Position.Id,
            carrier.Position.Value.ToString()!));
        AddCarriersImageToVertexIf(carrier.Position.Value != 0, carrier.Position.Id);

        carrier.MoveTo(carrier.CurrentRoute.Dequeue());
        carrier.Position.Value = (carrier.Position.Value ?? 0) + 1;
        problemRecreationCommands?.Add(new ChangeVertexValueCommand(carrier.Position.Id,
            carrier.Position.Value.ToString()!));

        AddCarriersImageToVertexIf(carrier.Position.Value != 0, carrier.Position.Id);
    }

    private void AddCarriersImageToVertexIf(bool expression, int index)
    {
        if (expression && _factoryIndex != index)
        {
            problemRecreationCommands?.Add(new ChangeVertexImageCommand(index, GraphVertexImages.PlaszczakiFence));
        }
        else
        {
            if (_factoryIndex != index)
                problemRecreationCommands?.Add(new RemoveVertexImageCommand(index));
        }
    }

    private void ReturnCarrierToFactory(Carrier carrier, FenceTransportInputData data)
    {
        var factoryVertex = data.Vertices[_factoryIndex];
        var path = FindShortestPathToVertex(carrier.Position, factoryVertex, data);
        foreach (var vertex in path) carrier.CurrentRoute.Enqueue(vertex);
        carrier.State = CarrierState.Reffiling;
    }

    private void AssignCarrierVertexToBuild(Carrier carrier, FenceTransportInputData data)
    {
        var furthestVertex = FindFurthestUnfinishedFenceVertex(carrier.Position, data);
        var path = FindShortestPathToVertex(carrier.Position, furthestVertex, data);
        foreach (var vertex in path) carrier.CurrentRoute.Enqueue(vertex);
        carrier.State = CarrierState.Delivering;
    }

    private ProblemVertex? GetSecondEndOfEdge(ProblemVertex current, ProblemEdge edge, List<ProblemVertex> vertices)
    {
        if (edge.From == current.Id)
            return vertices.First(v => v.Id == edge.To);
        if (edge.To == current.Id)
            return vertices.First(v => v.Id == edge.From);
        return null;
    }

    private List<int> AddHullEdges(List<ProblemVertex> vertices,
        List<ProblemEdge> edges, List<int> HullIndexes)
    {
        List<int> HullEdgesIndexes = new();
        for (var i = 0; i < HullIndexes.Count; i++)
        {
            var from = HullIndexes[i];
            var to = HullIndexes[(i + 1) % HullIndexes.Count];
            HullEdgesIndexes.Add(edges.Count);
            edges.Add(new ProblemEdge(edges.Count, from, to,
                new ProblemGraphThroughput(0, CalculateLengthBetweenHullVerticies(vertices[from], vertices[to]))));
        }

        return HullEdgesIndexes;
    }

    private int CalculateLengthBetweenHullVerticies(ProblemVertex vertex1, ProblemVertex vertex2)
    {
        var x1 = vertex1.X.GetValueOrDefault();
        var y1 = vertex1.Y.GetValueOrDefault();
        var x2 = vertex2.X.GetValueOrDefault();
        var y2 = vertex2.Y.GetValueOrDefault();
        return (int)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
    }

    private List<ProblemVertex> GetUnfinishedVertieces(FenceTransportInputData data)
    {
        List<ProblemVertex> toReturn = [];
        foreach (var vertex in GetConvexHullVertices(data))
            if (GetUnfifinishedEdgesConnectedToVertex(vertex, data).Count > 0)
                toReturn.Add(vertex);
        return toReturn;
    }

    private List<ProblemVertex> GetConvexHullVertices(FenceTransportInputData data)
    {
        return data.Vertices.Where(v => data.ConvexHullOutput?.HullIndexes?.Contains(v.Id) ?? false).ToList();
    }

    private List<ProblemEdge> GetUnfifinishedEdgesConnectedToVertex(ProblemVertex vertex, FenceTransportInputData data)
    {
        List<ProblemEdge> toReturn = [];
        foreach (var edge in data.Edges)
        {
            if (!(vertex.Id == edge.From || vertex.Id == edge.To))
                continue;

            if (!(edge.Throughput is not null &&
                  edge.Throughput.Flow != edge.Throughput.Capacity))
                continue;

            toReturn.Add(edge);
        }

        return toReturn;
    }

    private ProblemVertex FindFurthestUnfinishedFenceVertex(ProblemVertex FactoryVertex, FenceTransportInputData data)
    {
        var HullIndexes = data.ConvexHullOutput!.HullIndexes!;
        var vertices = data.Vertices;
        var distances = GetDistancesToEachVertex(FactoryVertex, data);
        var maxDistance = 0;
        var maxIndex = 0;
        var UnfinishedVertices = GetUnfinishedVertieces(data);
        for (var i = 0; i < HullIndexes.Count; i++)
            if (distances[HullIndexes[i]] > maxDistance && UnfinishedVertices.Contains(vertices[HullIndexes[i]]))
            {
                maxDistance = distances[HullIndexes[i]];
                maxIndex = HullIndexes[i];
            }

        return vertices[maxIndex];
    }

    private List<int> GetDistancesToEachVertex(ProblemVertex start, FenceTransportInputData data)
    {
        var vertices = data.Vertices;
        var edges = data.Edges;
        List<int> distances = new();
        for (var i = 0; i < vertices.Count; i++) distances.Add(int.MaxValue);
        Queue<ProblemVertex> queue = new();
        queue.Enqueue(start);
        distances[start.Id] = 0;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in edges)
            {
                if (edge.From == current.Id && distances[edge.To] == int.MaxValue)
                {
                    distances[edge.To] = distances[edge.From] + 1;
                    queue.Enqueue(vertices[edge.To]);
                }

                if (edge.To == current.Id && distances[edge.From] == int.MaxValue)
                {
                    distances[edge.From] = distances[edge.To] + 1;
                    queue.Enqueue(vertices[edge.From]);
                }
            }
        }

        return distances;
    }

    private List<ProblemVertex> FindShortestPathToVertex(ProblemVertex start, ProblemVertex end,
        FenceTransportInputData data)
    {
        var distances = GetDistancesToEachVertex(start, data);
        var path = new List<ProblemVertex>();
        var current = end;
        while (current != start)
        {
            path.Add(current);
            foreach (var edge in data.Edges)
            {
                if (edge.From == current.Id && distances[edge.To] == distances[current.Id] - 1)
                {
                    current = data.Vertices[edge.To];
                    break;
                }

                if (edge.To == current.Id && distances[edge.From] == distances[current.Id] - 1)
                {
                    current = data.Vertices[edge.From];
                    break;
                }
            }
        }

        path.Add(start);

        path.Reverse();
        return path;
    }
}