namespace Infrastucture.Configurations;

public sealed record EmailOptions(
    string FromAddress,
    string FromName,
    string LogoUrl,
    string ProjectName);
