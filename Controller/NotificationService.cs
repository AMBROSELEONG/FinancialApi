using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FinancialApi.Services;

namespace FinancialApi.Controller
{
    public class NotificationService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _services;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IServiceProvider services, ILogger<NotificationService> logger)
        {
            _services = services;
            _logger = logger;
            _timer = new Timer(CheckLastLoginTime, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
            _logger.LogInformation("Notification service started.");
            return Task.CompletedTask;
        }

        private async void CheckLastLoginTime(object? state)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                    var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                    var users = await userService.GetInactiveUsersAsync(now.AddHours(-24));

                    foreach (var user in users)
                    {
                        await SendPushNotificationAsync(user.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking last login time.");
            }
        }

        private async Task SendPushNotificationAsync(string token)
        {
            if (FirebaseMessaging.DefaultInstance != null)
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = "Reminder",
                        Body = "Remember to manage your finances today"
                    }
                };

                try
                {
                    await FirebaseMessaging.DefaultInstance.SendAsync(message);
                    _logger.LogInformation("Push notification sent to token: {Token}", token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending push notification to token: {Token}", token);
                }
            }
            else
            {
                _logger.LogWarning("FirebaseMessaging.DefaultInstance is null.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            _logger.LogInformation("Notification service stopped.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
