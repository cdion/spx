namespace Spx.Web.Services;

public interface IHelloService
{
    Task<string> SayHelloAsync(string userId, string name);
}