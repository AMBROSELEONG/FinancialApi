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
    public class DebtNotificationService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _services;
        private readonly ILogger<DebtNotificationService> _logger;

        public DebtNotificationService(IServiceProvider services, ILogger<DebtNotificationService> logger)
        {
            _services = services;
            _logger = logger;
            _timer = new Timer(CheckDebtsDueInDays, null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Change(TimeSpan.Zero, TimeSpan.FromDays(1));
            _logger.LogInformation("Debt notification service started.");
            return Task.CompletedTask;
        }

        private async void CheckDebtsDueInDays(object? state)
        {
            try
            {
                using (var scope = _services.CreateScope())
                {
                    var debtService = scope.ServiceProvider.GetRequiredService<DebtService>();
                    var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);

                    var debts = await debtService.GetDebtsDueInDaysAsync(7);

                    foreach (var debt in debts)
                    {
                        var daysLeft = (debt.NextDate - now).Days;
                        await SendDebtPushNotificationAsync(debt.Token, daysLeft);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking debts due in days.");
            }
        }

        private async Task SendDebtPushNotificationAsync(string token, int daysLeft)
        {
            if (FirebaseMessaging.DefaultInstance != null)
            {
                var message = new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = "Payment Reminder",
                        Body = $"You have {daysLeft} days left to make your payment."
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
            _logger.LogInformation("Debt notification service stopped.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
