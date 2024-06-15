﻿using Microsoft.AspNetCore.Components;
using ProblemResolver;
using ProjektZaliczeniowy_AiSD2.States;

namespace ProjektZaliczeniowy_AiSD2.Components.Pages.Output;

public class ProblemOutputBase<TProblemOutput>  : ComponentBase
    where TProblemOutput : ProblemOutput
{
    [Parameter] public required string ProblemName { get; set; }

    [Inject] protected IProblemState? ProblemState { get; set; }
    
    protected TProblemOutput? Output;

    protected override void OnInitialized()
    {
        if (ProblemState is null)
            throw new NullReferenceException("ProblemState is null");
        
        Output = ProblemState.GetProblemOutputData<TProblemOutput>();
    }
}
