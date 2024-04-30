using Microsoft.AspNetCore.Components;
using ProblemResolver;
using ProblemVisualizer;
using ProjektZaliczeniowy_AiSD2.Components.States;

namespace ProjektZaliczeniowy_AiSD2.Components.Pages.Problems;

public abstract class ProblemComponentBase<TInputData, TOutputData, TDrawData> : ComponentBase
    where TInputData : ProblemInputData
    where TOutputData : ProblemOutput
    where TDrawData : ICloneable
{
    [Inject] 
    private IProblemState? ProblemState { get; set; }

    public TInputData? InputData;

    protected TOutputData? OutputData;

    protected ProblemResolver<TInputData, TOutputData, TDrawData>? Resolver;
    
    protected ProblemRecreationCommands<TDrawData> RecreationCommands = new();

    protected ProblemVisualizerExecutor<TInputData, TDrawData>? Executor;

    protected FirstSnapshotCreator<TInputData, TDrawData>? FirstSnapshotCreator;

    protected override async Task OnInitializedAsync()
    {
        await ResolveInputDataFromSessionStorage();
        InitializeResolver();
        InitializeFirstSnapshotCreator(InputData!);
        ResolveAndCreateSnapshots();
    }

    protected async Task ResolveInputDataFromSessionStorage()
    {
        if (ProblemState is null)
            throw new NullReferenceException("ProblemState can't be null.");
        InputData = await ProblemState.GetProblemInputData<TInputData>();
    }

    protected void ResolveAndCreateSnapshots()
    {
        ResolveProblem();
        CreateDrawerDataSnapshots();
    }
    
    protected void ResolveProblem()
    {
        if (Resolver is null)
            throw new NullReferenceException("You need to initialize Resolver before resolving problem.");
        
        OutputData = Resolver.Resolve(InputData!, ref RecreationCommands);
    }

    protected void CreateDrawerDataSnapshots()
    {
        if (FirstSnapshotCreator is null)
            throw new NullReferenceException(
                "You need to initialize FirstSnapshotCreator before creating drawer snapshots.");
        Executor = new(RecreationCommands.Commands, FirstSnapshotCreator);
        Executor.CreateFirstSnapshot();
        Executor.ExecuteCommands();
    }

    protected abstract void InitializeResolver();

    protected abstract void InitializeFirstSnapshotCreator(TInputData inputData);

    protected ProblemVisualizerSnapshots<TDrawData> GetSnapshots()
    {
        return Executor!.GetSnapshots();
    }    
}