namespace Kudu.Core.Tracing
{
    public interface IAnalytics
    {
        void ProjectDeployed(string projectType, string result, long deploymentDurationInMilliseconds);
    }
}
