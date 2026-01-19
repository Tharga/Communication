namespace Tharga.Communication.Client;

internal class InstanceService : IInstanceService
{
    public InstanceService()
    {
        AgentInstanceKey = Guid.NewGuid();
    }

    public Guid AgentInstanceKey { get; }
}