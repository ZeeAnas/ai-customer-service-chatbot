using System.Globalization;
using System.Text.Encodings.Web;
using Chatbot.Api.Configuration;
using Chatbot.Api.Interfaces;
using Chatbot.Api.Models.Entities;
using Microsoft.Extensions.Options;
using Resend;

namespace Chatbot.Api.Services;

public sealed class LeadNotificationService
    : ILeadNotificationService
{
    private readonly IResend _resend;
    private readonly LeadNotificationOptions _options;
    private readonly ILogger<LeadNotificationService> _logger;

    public LeadNotificationService(
        IResend resend,
        IOptions<LeadNotificationOptions> options,
        ILogger<LeadNotificationService> logger
    )
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyNewLeadAsync(
        Lead lead,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(lead);

        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "Lead notification email is disabled for lead {LeadId}.",
                lead.Id
            );

            return;
        }

        try
        {
            var email = new EmailMessage
            {
                From = $"{_options.FromName} <{_options.FromEmail}>",
                Subject = $"New customer contact request — Lead #{lead.Id}",
                TextBody = BuildTextBody(lead),
                HtmlBody = BuildHtmlBody(lead)
            };

            email.To.Add(_options.RecipientEmail);

            await _resend.EmailSendAsync(
                email,
                cancellationToken
            );

            _logger.LogInformation(
                "Lead notification email sent for lead {LeadId}.",
                lead.Id
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Lead notification email was cancelled for lead {LeadId}.",
                lead.Id
            );
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Lead notification email failed for lead {LeadId}.",
                lead.Id
            );
        }
    }

    private static string BuildTextBody(Lead lead)
    {
        return $"""
            New customer contact request

            Lead ID: {lead.Id}
            Name: {DisplayValue(lead.Name)}
            Email: {DisplayValue(lead.Email)}
            Phone: {DisplayValue(lead.Phone)}
            Message: {DisplayValue(lead.Message)}
            Submitted: {lead.CreatedAtUtc.ToString(
                "yyyy-MM-dd HH:mm 'UTC'",
                CultureInfo.InvariantCulture
            )}
            """;
    }

    private static string BuildHtmlBody(Lead lead)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <body>
                <h2>New customer contact request</h2>
                <p><strong>Lead ID:</strong> {lead.Id}</p>
                <p><strong>Name:</strong> {Encode(lead.Name)}</p>
                <p><strong>Email:</strong> {Encode(lead.Email)}</p>
                <p><strong>Phone:</strong> {Encode(lead.Phone)}</p>
                <p><strong>Message:</strong> {Encode(lead.Message)}</p>
                <p>
                    <strong>Submitted:</strong>
                    {lead.CreatedAtUtc.ToString(
                        "yyyy-MM-dd HH:mm 'UTC'",
                        CultureInfo.InvariantCulture
                    )}
                </p>
            </body>
            </html>
            """;
    }

    private static string DisplayValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : value;
    }

    private static string Encode(string? value)
    {
        return HtmlEncoder.Default.Encode(
            DisplayValue(value)
        );
    }
}