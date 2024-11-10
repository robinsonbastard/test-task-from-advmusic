namespace test;

public sealed class DomainStateModel
{
    public DateTimeOffset? LastSuccessfulCheckDateTime { get; set; }
    
    public bool LastState { get; set; }
}