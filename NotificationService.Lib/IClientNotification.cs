using System.Threading.Tasks;

namespace NotificationService.Lib
{
    public interface IClientNotification
    {
        Task Notify(string message);
        Task GroupBroadcast(string message);
    }
}
