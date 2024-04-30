using ProblemVisualizer;

namespace ProblemResolver;

public class ProblemRecreationCommands<TDrawData>
    where TDrawData : ICloneable
{
    public List<ProblemVisualizerCommandsQueue<TDrawData>> Commands;
    
    public ProblemRecreationCommands()
    {
        Commands = [new ProblemVisualizerCommandsQueue<TDrawData>()];
    }

    public void NextStep()
    {
        Commands.Add(new ProblemVisualizerCommandsQueue<TDrawData>());
    }

    public void Add(ProblemVisualizerCommand<TDrawData> command)
    {
        Commands.Last().Enqueue(command);
    }
}