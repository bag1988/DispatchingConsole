using SharedLibrary.GlobalEnums.License;

namespace SharedLibrary.Models.License;

public class LicenseInfo
{
    public string? CheckTime { get; set; } //время проверки(не отображается)

    public LicenseFuture FutureID { get; set; } //наименование

    public string? KeyID { get; set; } //ключ

    public bool KeyPresent { get; set; } //извлечен

    public LicenseKeyType KeyType { get; set; } //тип

    public int LicenseCount { get; set; } //параметр

    public int TimeoutLicense { get; set; } //время без ключа()

    public string? ValidTime { get; set; } //время окончания

    public string? MemoryData { get; set; }

    public static LicenseServiceProto.V1.LicenseInformation ToLicenseInformation(LicenseInfo licenseInfo) =>
        new () {
            CheckTime = licenseInfo.CheckTime
          , FutureId = (int) licenseInfo.FutureID
          , KeyId = licenseInfo.KeyID ?? string.Empty
          , KeyPresent = licenseInfo.KeyPresent
          , KeyType = (int) licenseInfo.KeyType
          , LicenseCount = licenseInfo.LicenseCount
          , TimeoutLicense = licenseInfo.TimeoutLicense
          , ValidTime = licenseInfo.ValidTime ?? string.Empty
          , MemoryData = licenseInfo.MemoryData ?? string.Empty
        };

    public static LicenseInfo FromLicenseInformation(LicenseServiceProto.V1.LicenseInformation licenseInformation) =>
        new () {
            CheckTime = licenseInformation.CheckTime
          , FutureID = (LicenseFuture) licenseInformation.FutureId
          , KeyID = licenseInformation.KeyId
          , KeyPresent = licenseInformation.KeyPresent
          , KeyType = (LicenseKeyType) licenseInformation.KeyType
          , LicenseCount = licenseInformation.LicenseCount
          , TimeoutLicense = licenseInformation.TimeoutLicense
          , ValidTime = licenseInformation.ValidTime
          , MemoryData = licenseInformation.MemoryData
        };
}
