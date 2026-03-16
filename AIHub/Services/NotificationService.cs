using System;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AIHub.Services
{
    public interface INotificationService
    {
        void ShowNotification(string title, string message);
    }

    public class NotificationService : INotificationService
    {
        public void ShowNotification(string title, string message)
        {
            try
            {
                // To keep this compatible without Microsoft.Toolkit.Uwp.Notifications,
                // we'll invoke the dispatcher to show a simple WPF message box occasionally,
                // or just log it to debug
                System.Diagnostics.Debug.WriteLine($"[NOTIFICATION] {title}: {message}");
            }
            catch
            {
                // Suppress
            }
        }
    }
}
