namespace SharedLibrary;

public record ProductVersion(
    string CompanyName,
    string ProductName,
    string ProductVersionNumberMajor,
    string ProductVersionNumberMinor,
    string ProductVersionNumberPatch,
    string BuildNumber
);
