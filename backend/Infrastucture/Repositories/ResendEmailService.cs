using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Infrastucture.Configurations;
using Resend;

namespace Infrastucture.Repositories;

public sealed class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly string _logoUrl;
    private readonly string _projectName;

    public ResendEmailService(IResend resend, EmailOptions options)
    {
        _resend = resend;
        _fromAddress = options.FromAddress;
        _fromName = options.FromName;
        _logoUrl = options.LogoUrl;
        _projectName = options.ProjectName;
    }

    public async Task SendAsync(EmailJobMessage message, CancellationToken ct = default)
    {
        var emailMessage = new EmailMessage
        {
            From = BuildFromAddress(),
            Subject = BuildSubject(message),
            HtmlBody = BuildHtmlBody(message),
            TextBody = BuildTextBody(message)
        };

        emailMessage.To.Add(message.Email);
        await _resend.EmailSendAsync(emailMessage, ct);
    }

    private string BuildFromAddress()
    {
        return string.IsNullOrWhiteSpace(_fromName)
            ? _fromAddress
            : $"{_fromName} <{_fromAddress}>";
    }

    private static string BuildSubject(EmailJobMessage job)
    {
        return job.Type switch
        {
            EmailJobType.VerifyEmail => "Verify your Shorth account",
            EmailJobType.ForgotPassword => "Reset your Shorth password",
            _ => throw new InvalidOperationException($"Unsupported email job type: {job.Type}.")
        };
    }

    private static string BuildTextBody(EmailJobMessage job)
    {
        var greetingName = string.IsNullOrWhiteSpace(job.DisplayName) ? "there" : job.DisplayName;
        var otpCode = job.OtpCode ?? throw new InvalidOperationException("Otp code is required for this email job.");

        return job.Type switch
        {
            EmailJobType.VerifyEmail =>
                $"Hi {greetingName},\n\nUse this verification code to activate your account: {otpCode}\n\nThis code expires in 10 minutes.",
            EmailJobType.ForgotPassword =>
                $"Hi {greetingName},\n\nUse this code to reset your Shorth password: {otpCode}\n\nThis code expires in 10 minutes.",
            _ => throw new InvalidOperationException($"Unsupported email job type: {job.Type}.")
        };
    }

    private string BuildHtmlBody(EmailJobMessage job)
    {
        var greetingName = string.IsNullOrWhiteSpace(job.DisplayName) ? "there" : job.DisplayName;
        var otpCode = job.OtpCode ?? throw new InvalidOperationException("Otp code is required for this email job.");
        var title = job.Type switch
        {
            EmailJobType.VerifyEmail => "Verify your email",
            EmailJobType.ForgotPassword => "Reset your password",
            _ => throw new InvalidOperationException($"Unsupported email job type: {job.Type}.")
        };
        var bodyText = job.Type switch
        {
            EmailJobType.VerifyEmail => "Use the code below to finish creating your account.",
            EmailJobType.ForgotPassword => "Use the code below to continue resetting your password.",
            _ => throw new InvalidOperationException($"Unsupported email job type: {job.Type}.")
        };

        return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="utf-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                  <title>{title}</title>
                </head>
                <body style="margin:0;padding:24px 0;background-color:#f5f5f5;font-family:Arial,Helvetica,sans-serif;color:#202124;">
                  <div style="max-width:760px;margin:0 auto;padding:0 16px;">
                    <div style="background:#ffffff;border:1px solid #d0d7de;border-radius:22px;padding:36px 40px;">
                      <div style="text-align:center;margin-bottom:24px;">
                        <img src="{System.Net.WebUtility.HtmlEncode(_logoUrl)}"
                             alt="{System.Net.WebUtility.HtmlEncode(_projectName)} logo"
                             style="display:block;margin:0 auto 12px;max-width:72px;height:auto;" />
                        <div style="font-size:18px;font-weight:700;color:#202124;">
                          {System.Net.WebUtility.HtmlEncode(_projectName)}
                        </div>
                      </div>
                      <h1 style="margin:0 0 18px;font-size:40px;line-height:1.18;font-weight:500;color:#202124;">
                        {title}
                      </h1>
                      <div style="height:1px;background:#dadce0;margin:0 0 28px;"></div>
                      <p style="margin:0 0 14px;font-size:17px;line-height:1.65;color:#202124;">
                        Hi {System.Net.WebUtility.HtmlEncode(greetingName)},
                      </p>
                      <p style="margin:0 0 30px;font-size:17px;line-height:1.65;color:#202124;">
                        {bodyText}
                      </p>
                      <div style="margin:0 0 30px;text-align:center;">
                        <div style="font-size:68px;line-height:1;font-weight:500;letter-spacing:0.08em;color:#202124;">
                          {System.Net.WebUtility.HtmlEncode(otpCode)}
                        </div>
                      </div>
                      <p style="margin:0 0 12px;font-size:16px;line-height:1.65;color:#3c4043;">
                        This code expires in 10 minutes.
                      </p>
                      <p style="margin:0;font-size:16px;line-height:1.65;color:#3c4043;">
                        If you did not request this, you can safely ignore this email.
                      </p>
                    </div>
                  </div>
                </body>
                </html>
                """;
    }
}
