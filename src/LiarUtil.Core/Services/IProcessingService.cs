namespace LiarUtil.Core.Services;

public interface IProcessingService
{
    Task ProcessAsync(ProcessingRequest request);
}
