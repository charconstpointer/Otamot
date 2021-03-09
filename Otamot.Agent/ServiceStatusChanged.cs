namespace Otamot.Agent
{
    public class ServiceStatusChanged : IServiceEvent
    {
        public Service Service { get; set; }
    }

    public interface IServiceEvent
    {
        Service Service { get; set; }
    }
}