using SharedLibrary.Models;

namespace SensorM.GsoCommon.ServerLibrary.HubsProvider
{
    public interface IExtensionsHub
    {
        Task Fire_StartSessionSubCu(GateServiceProto.V1.CUStartSitInfo request);

        Task Fire_ShowMessage(SharedLibrary.Models.MessageToShow request);

        Task Fire_SetAcceptanceNotificationProcessed(NotificationAcceptanceProcessed request);

        Task Fire_AskForAcceptanceNotification(NotificationAcceptanceRequest request);

        Task Fire_ForbiddenConnect();

        Task Fire_ChangeLanguage(string Value)
        {
            return Task.CompletedTask;
        }
    }
}
