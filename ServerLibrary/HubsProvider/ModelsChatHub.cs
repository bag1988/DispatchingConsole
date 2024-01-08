using SharedLibrary.Models;
using SharedLibrary.Utilities;

namespace ServerLibrary.HubsProvider
{

    public class ReplicateFilesRequest()
    {
        public MyConnectInfo? RemoteUrl { get; set; }
        public Guid? KeyChatRoom { get; set; }
        public string? Message { get; set; }
        public string? FileUrl { get; set; }
        public string[]? UserNames { get; set; }
    }

    public enum TypeAnswerForJoin
    {
        Error,//ошибка
        Busy,//занято, идет другой вызов
        OtherDevice,//ответило другое устройство
        LostConnect,//потерянное соединение
        Active,//комната активна, можно присоедениться
        Ready//готов для создания P2P
    }
       
    public class MyConnectInfo
    {
        public MyConnectInfo() { }
        public MyConnectInfo(string? ipAddress, string? userName)
        {
            var p = IpAddressUtilities.ParseUri(ipAddress);
            HostName = p.Host;
            AuthorityUrl = p.Authority;
            UserName = userName;
        }
        public string? HostName { get; init; }
        public string? AuthorityUrl { get; init; }
        public string? UserName { get; init; }
    }

    public struct JoinModel
    {
        public JoinModel(ChatInfo connect, MyConnectInfo? remoteUrl, MyConnectInfo? forUrl)
        {
            Connect = connect;
            foreach (var item in Connect.Items)
            {
                item.State = StateCall.Disconnect;
            }
            RemoteUrl = remoteUrl;
            ForUrl = forUrl;
        }

        public ChatInfo Connect { get; set; }
        public MyConnectInfo? RemoteUrl { get; set; }
        public MyConnectInfo? ForUrl { get; set; }
    }

    public struct AnswerForJoin
    {
        public Guid KeyChatRoom { get; set; }
        public MyConnectInfo? RemoteUrl { get; set; }
        public MyConnectInfo? ForUrl { get; set; }
        public TypeAnswerForJoin TypeAnswer { get; set; }
    }

    public struct KeyChatForUrl
    {
        public Guid KeyChatRoom { get; set; }
        public MyConnectInfo? RemoteUrl { get; set; }
        public MyConnectInfo? ForUrl { get; set; }
    }

    public struct KeyChatForUrlAndValue
    {
        public Guid KeyChatRoom { get; set; }
        public MyConnectInfo? RemoteUrl { get; set; }
        public MyConnectInfo? ForUrl { get; set; }
        public string Value { get; set; }
    }
}
