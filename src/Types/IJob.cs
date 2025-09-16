namespace Michelangelo.Types;

public interface IJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
