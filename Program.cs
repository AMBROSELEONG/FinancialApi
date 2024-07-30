using System.Net;
using System.Net.Mail;
using FinancialApi.Controller;
using FinancialApi.Data;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FinancialApi.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddSingleton<SmtpClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var smtpClient = new SmtpClient
    {
        Host = configuration["Smtp:Host"],
        Port = int.Parse(configuration["Smtp:Port"]),
        EnableSsl = bool.Parse(configuration["Smtp:EnableSsl"]),
        Credentials = new NetworkCredential(
            configuration["Smtp:Username"],
            configuration["Smtp:Password"]
        )
    };
    return smtpClient;
});

builder.Services.AddHostedService<NotificationService>();
builder.Services.AddHostedService<DebtNotificationService>();

builder.Services.AddScoped<DebtService>();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

var credentialsPath = app.Configuration["Firebase:CredentialsPath"];

// Initialize Firebase
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(credentialsPath)
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
