using System.Threading.Tasks;

namespace GridironGM.Client.Api
{
    public interface IBackendClient
    {
        Task<(int status, string body)> GetAsync(string path);
        Task<(int status, string body)> PostAsync(string path, string jsonBody = "{}");
    }
}
