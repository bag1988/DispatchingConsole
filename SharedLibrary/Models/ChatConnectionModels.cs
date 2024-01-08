using System;
using System.Text;
using SharedLibrary.Utilities;

namespace SharedLibrary.Models
{

    public class ContactInfo
    {
        public ContactInfo()
        {
            Name = string.Empty;
            AuthorityUrl = string.Empty;
            UserName = string.Empty;
            StaffId = 0;
        }

        public ContactInfo(string name, string authorityUrl, string userName, int staffId, DateTime? lastActive = null)
        {
            Name = name;
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
            UserName = userName;
            StaffId = staffId;
            LastActive = lastActive;
        }

        public string Name { get; init; }

        public string AuthorityUrl { get; init; }

        public string UserName { get; init; }

        public int StaffId { get; init; }

        public DateTime? LastActive { get; set; }

        public bool IsActive => DateTime.UtcNow < LastActive?.AddMinutes(2);

        public TypeContact Type { get; set; }
    }
       
    public class ChatMessagesForUser
    {
        public ChatMessagesForUser(string authorityUrl, string userName)
        {
            UserName = userName;
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
        }
        public string AuthorityUrl { get; init; }
        public string UserName { get; init; }
        public List<ChatForRoom> Messages { get; set; } = new();
    }

    public class ChatForRoom
    {
        public ChatForRoom(Guid keyChat)
        {
            KeyChat = keyChat;
        }
        public Guid KeyChat { get; init; }
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class ChatMessages
    {
        public ChatMessages(string userName, Guid keyChat)
        {
            UserName = userName;
            KeyChat = keyChat;
        }
        public Guid KeyChat { get; init; }
        public string UserName { get; init; }
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class MessagesAndAllCount
    {
        public List<ChatMessage> Messages { get; set; } = new();
        public int AllCount { get; set; } = 0;
    }


    public struct ChatMessage
    {
        public ChatMessage(string? authorityUrl, string userName, string message)
        {
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
            Message = message;
            Date = DateTime.UtcNow;
            UserName = userName;
        }

        public ChatMessage(string? authorityUrl, string userName, string message, string fileName)
        {
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
            Message = message;
            Date = DateTime.UtcNow;
            UserName = userName;
            Url = fileName;
        }
        /// <summary>
        /// Кто отправил сообщение
        /// </summary>
        public string AuthorityUrl { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class ChatForUser
    {
        public ChatForUser(string authorityUrl, string userName)
        {
            UserName = userName;
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
        }

        public string UserName { get; init; }
        public string AuthorityUrl { get; init; }
        public List<ChatInfo> Chats { get; set; } = new();
    }

    public class ChatInfo
    {
        public ChatInfo()
        {
            AuthorityCreate = string.Empty;
            UserCreate = string.Empty;
        }

        public ChatInfo(string nameRoom, IEnumerable<ConnectInfo> items, string authorityCreate, string userCreate, bool isDefault)
        {
            NameRoom = nameRoom;
            Items.AddRange(items);
            AuthorityCreate = IpAddressUtilities.GetAuthority(authorityCreate);
            UserCreate = userCreate;
            IsDefault = isDefault;
        }

        public ChatInfo(ChatInfo other)
        {
            Key = other.Key;
            NameRoom = other.NameRoom;
            Items.AddRange(other.Items.Select(x => new ConnectInfo(x.AuthorityUrl, x.UserName)));
            IsDefault = other.IsDefault;
            AuthorityCreate = other.AuthorityCreate;
            UserCreate = other.UserCreate;
            IdUiConnect = other.IdUiConnect;
            AuthorityCreate = IpAddressUtilities.GetAuthority(other.AuthorityCreate);
        }

        public Guid Key { get; init; } = Guid.NewGuid();
        public string NameRoom { get; set; } = string.Empty;
        public List<ConnectInfo> Items { get; init; } = new();
        public bool IsDefault { get; init; }
        public string AuthorityCreate { get; init; }
        public string UserCreate { get; init; }
        public Guid? IdUiConnect { get; set; }
        public TypeConnect OutTypeConn { get; set; } = TypeConnect.Message;
    }

    public class ConnectInfo
    {
        public ConnectInfo()
        {

        }

        public ConnectInfo(string? authorityUrl, string? userName)
        {
            AuthorityUrl = IpAddressUtilities.GetAuthority(authorityUrl);
            UserName = userName;
        }
        public string? AuthorityUrl { get; set; }
        public StateCall State { get; set; } = StateCall.Disconnect;
        public string? UserName { get; set; }
    }

    public enum StateCall
    {
        Error,
        ErrorCreateHub,
        Aborted,
        OtherDevice,
        Disconnect,

        #region Ok status
        Calling = 200,
        CreateAnswer,
        CreateP2P,
        Connected,
        ChangeStream
        #endregion
    }

    public enum TypeCancelCall
    {
        Cancelled,
        Busy
    }

    public enum TypeConnect
    {
        Message,
        Sound,
        Video,
        Screen
    }

    public enum TypeShare
    {
        Sound,
        VideoOn,
        VideoOff,
        ScreenOn,
        ScreenOff
    }


    public enum StateRecord
    {
        Hide,
        Show
    }

    public enum TypeContact
    {
        Unknow,
        Local,
        Remote,
        Manual
    }
}
