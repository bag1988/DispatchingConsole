using Grpc.Core;
using PodsProto.V1;
using Microsoft.Extensions.Logging;
using SensorM.GsoCommon.ServerLibrary.Context;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Extensions;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using LibraryProto.Helpers.V1;
using FiltersGSOProto.V1;
namespace SensorM.GsoCommon.ServerLibrary.Services
{
    public class PodsServiceV1 : PodsService.PodsServiceBase, IDisposable
    {
        private readonly ILogger<PodsServiceV1> _logger;
        private readonly PodsContext _DbContext;
        readonly string DirectoryTmp;

        public static ContactInfo? SetUpdateActiveForContact = null;

        public PodsServiceV1(ILogger<PodsServiceV1> logger, IConfiguration conf, PodsContext dbContext)
        {
            _logger = logger;
            DirectoryTmp = conf.GetValue<string?>("PodsArhivePath") ?? "PodsArhivePath";
            _DbContext = dbContext;
        }


        int GetIdUser(string authorityUrl, string userName)
        {
            try
            {
                if (!string.IsNullOrEmpty(authorityUrl) && !string.IsNullOrEmpty(userName))
                {
                    if (!_DbContext.Users.Any(x => x.UserName == userName))
                    {
                        _logger.LogTrace(@"Добавление пользователя {Url}:{UserName}", authorityUrl, userName);
                        var user = new UserIdentity()
                        {
                            AuthorityUrl = authorityUrl,
                            UserName = userName
                        };
                        _DbContext.Users.Add(user);
                        _DbContext.SaveChanges();
                    }

                    UpdateLastActiveLocalContact(new ContactInfo()
                    {
                        AuthorityUrl = authorityUrl,
                        UserName = userName,
                        LastActive = DateTime.UtcNow.ToTimestamp()
                    }).Wait();

                    return _DbContext.Users.First(x => x.UserName == userName).Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return 0;
        }

        public override Task<ChatsList> GetChats(UserIdentity request, ServerCallContext context)
        {
            ChatsList response = new();
            try
            {
                var idUser = GetIdUser(request.AuthorityUrl, request.UserName);
                if (idUser > 0)
                {
                    var query = from chat in _DbContext.Chats.Include(x => x.Items).AsNoTracking().AsEnumerable().Where(x => x.UserIdentityId == idUser)
                                join p in _DbContext.Messages.AsNoTracking() on chat.Id equals p.ChatInfoId into grouping
                                select new { chat, LastMessage = grouping.Max(x => x.Date) };
                    response.List.AddRange(query.OrderByDescending(x => x.LastMessage).Select(x => x.chat));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        static Timestamp TimeStampToTimeStampOnlyDate(Timestamp? strData)
        {
            if (strData == null) return new();
            var d = strData.ToDateTime().ToLocalTime().Date.ToUniversalTime().Date;
            return d.ToTimestamp();
        }

        static TimeSpan? TimeStampToTimeOnly(Timestamp? strData)
        {
            if (strData == null) return new();
            var d = strData.ToDateTime().TimeOfDay;
            return new TimeSpan(d.Hours, d.Minutes, 0);
        }

        public override Task<MessagesForChat> GetMessagesForChat(RequestPaginatedOutput request, ServerCallContext context)
        {
            MessagesForChat response = new();
            try
            {
                if (request.Key?.TryUnpack<UserKeyAndChatKey>(out var model) ?? false)
                {
                    var idUser = GetIdUser(model.UserKey.AuthorityUrl, model.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var idChat = _DbContext.Chats.AsNoTracking().FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == model.ChatKey)?.Id;

                        Expression<Func<ChatMessage, bool>>? filtrExp = null;
                        if (request.Filtr?.TryUnpack<ViewPodsMessageFiltr>(out var filtr) ?? false)
                        {
                            var modelType = Expression.Parameter(ChatMessage.Descriptor.ClrType);

                            BinaryExpression? filter = null;

                            if (filtr.UserName?.Count > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.UserName));
                                filter = filtr.UserName.CreateExpressionFromRepeatedString(member, filter);
                            }
                            if (filtr.AuthorityUrl?.Count > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.AuthorityUrl));
                                filter = filtr.AuthorityUrl.CreateExpressionFromRepeatedString(member, filter);
                            }
                            if (filtr.Message?.Count > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.Message));
                                filter = filtr.Message.CreateExpressionFromRepeatedString(member, filter);
                            }
                            if (filtr.DateCreate?.Count > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.Date));
                                var uintToStringExp = Expression.Call(typeof(PodsServiceV1), nameof(TimeStampToTimeStampOnlyDate), null, member);
                                filter = filtr.DateCreate.CreateExpressionFromRepeatedDataOnly(uintToStringExp, filter);
                            }
                            if (filtr.TimeCreate?.Count > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.Date));
                                var uintToStringExp = Expression.Call(typeof(PodsServiceV1), nameof(TimeStampToTimeOnly), null, member);
                                filter = filtr.TimeCreate.CreateExpressionFromRepeatedTime(uintToStringExp, filter);
                            }
                            if (filtr.IsRecord != null)
                            {
                                if (filtr.IsRecord.Value == true)
                                {
                                    var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.Url));
                                    filter = filter.AddBinaryExpression(Expression.NotEqual(member, Expression.Constant("")));
                                }
                                else
                                {
                                    var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.Url));
                                    filter = filter.AddBinaryExpression(Expression.Equal(member, Expression.Constant("")));
                                }
                            }

                            if (idChat > 0)
                            {
                                var member = Expression.PropertyOrField(modelType, nameof(ChatMessage.ChatInfoId));
                                filter = filter.AddBinaryExpression(Expression.Equal(member, Expression.Constant(idChat)));


                                if (filter != null)
                                {
                                    filtrExp = Expression.Lambda<Func<ChatMessage, bool>>(filter, modelType);

                                    var r = _DbContext.Messages.AsNoTracking().Where(filtrExp.Compile()).OrderByDescending(x => x.Date);
                                    if (r?.Any() ?? false)
                                    {
                                        response.List.AddRange(r.Skip(request.Skip).Take(request.Take).Reverse());
                                        response.AllCount = r.Count();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override Task<CountNoReadMessageForUserList> GetCountNoReadMessages(UserIdentity request, ServerCallContext context)
        {
            CountNoReadMessageForUserList response = new();
            try
            {
                var idUser = GetIdUser(request.AuthorityUrl, request.UserName);
                if (idUser > 0)
                {

                    var query = from noRead in _DbContext.NoReadMessages.AsNoTracking().AsEnumerable().Where(x => x.UserIdentityId == idUser)
                                join p in _DbContext.Chats.AsNoTracking() on noRead.ChatInfoId equals p.Id
                                select new { noRead.Count, p.Key };

                    response.List.AddRange(query.Select(x => new CountNoReadMessageForUser()
                    {
                        ChatKey = x.Key,
                        Count = x.Count
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override async Task<BoolValue> RemoveNoReadMessages(UserKeyAndChatKey request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.UserKey != null)
                {
                    var idUser = GetIdUser(request.UserKey.AuthorityUrl, request.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var chatId = _DbContext.Chats.FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.ChatKey);

                        if (chatId != null)
                        {
                            var f = _DbContext.NoReadMessages.FirstOrDefault(x => x.UserIdentityId == idUser && x.ChatInfoId == chatId.Id);
                            if (f != null)
                            {
                                f.Count = 0;
                                await _DbContext.SaveChangesAsync();
                                response.Value = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> AddMessageForChat(NewMessage request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.Key?.UserKey != null)
                {
                    var idUser = GetIdUser(request.Key.UserKey.AuthorityUrl, request.Key.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var idChat = _DbContext.Chats.FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.Key.ChatKey)?.Id;

                        if (idChat > 0)
                        {
                            request.Message.ChatInfoId = idChat.Value;
                            _DbContext.Messages.Add(request.Message);

                            var f = _DbContext.NoReadMessages.FirstOrDefault(x => x.ChatInfoId == idChat && x.UserIdentityId == idUser);
                            if (f == null)
                            {
                                _DbContext.NoReadMessages.Add(new()
                                {
                                    ChatInfoId = idChat.Value,
                                    UserIdentityId = idUser,
                                    Count = 1
                                });
                            }
                            else
                            {
                                f.Count++;
                            }


                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> AddChat(ChatAndUserKey request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.UserKey != null)
                {
                    var idUser = GetIdUser(request.UserKey.AuthorityUrl, request.UserKey.UserName);

                    if (idUser > 0 && !_DbContext.Chats.Any(x => x.UserIdentityId == idUser && x.Key == request.Chat.Key))
                    {
                        _logger.LogTrace(@"Добавление чата {Name} для пользователя {Url}:{UserName}", request.Chat.NameRoom, request.UserKey.AuthorityUrl, request.UserKey.UserName);
                        request.Chat.UserIdentityId = idUser;
                        _DbContext.Chats.Add(request.Chat);
                        await _DbContext.SaveChangesAsync();
                        response.Value = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        async Task CreateDefaultChats(IEnumerable<ContactInfo> items)
        {
            try
            {
                List<string> noContains = new();

                List<UserIdentity> users = new();

                foreach (var contact in items)
                {
                    var user = new UserIdentity()
                    {
                        AuthorityUrl = contact.AuthorityUrl,
                        UserName = contact.UserName
                    };
                    user.Id = GetIdUser(user.AuthorityUrl, user.UserName);
                    users.Add(user);
                }

                foreach (var item in users)
                {
                    noContains.Add(item.UserName);
                    foreach (var item2 in _DbContext.Users.Where(x => !noContains.Contains(x.UserName)))
                    {
                        Guid keyChat = Guid.NewGuid();
                        ChatInfo chat = new()
                        {
                            IsDefault = true,
                            Key = keyChat.ToString(),
                            NameRoom = item2.UserName,
                            UserIdentityId = item.Id
                        };
                        chat.Items.AddRange([new ConnectInfo() { AuthorityUrl = item.AuthorityUrl, UserName = item.UserName }, new ConnectInfo() { AuthorityUrl = item2.AuthorityUrl, UserName = item2.UserName }]);

                        _DbContext.Chats.Add(chat);
                        _DbContext.Chats.Add(new(chat) { NameRoom = item.UserName, UserIdentityId = item2.Id });
                    }
                }
                await _DbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public override async Task<BoolValue> AddLocalContact(ContactInfoList request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.List?.Count > 0)
                {
                    var newData = request.List.ExceptBy(_DbContext.SharedContact.AsEnumerable().Where(x => x.Type == TypeContact.Local).Select(x => x.UserName), x => x.UserName).ToList();

                    var deleteData = _DbContext.SharedContact.AsEnumerable().Where(x => x.Type == TypeContact.Local).ExceptBy(request.List.Select(x => x.UserName), x => x.UserName);

                    if (newData?.Count > 0)
                    {
                        _DbContext.SharedContact.AddRange(newData);
                        await _DbContext.SaveChangesAsync();
                        await CreateDefaultChats(newData);
                        response.Value = true;
                    }
                    if (deleteData?.Any() ?? false)
                    {
                        foreach (var item in deleteData)
                        {
                            var user = _DbContext.Users.FirstOrDefault(x => x.UserName == item.UserName);

                            if (user != null)
                            {
                                var listChat = _DbContext.Chats.Where(x => x.UserIdentityId == user.Id);

                                foreach (var chat in listChat)
                                {
                                    RemoveFilesForChat(item.UserName, chat.Key, chat.NameRoom);
                                    _DbContext.Chats.Remove(chat);
                                }
                                var deleteContactItems = _DbContext.Connects.Where(x => x.AuthorityUrl == user.AuthorityUrl && x.UserName == user.UserName);
                                if (deleteContactItems?.Any() ?? false)
                                {
                                    _DbContext.Connects.RemoveRange(deleteContactItems);
                                }
                            }
                        }
                        _DbContext.SharedContact.RemoveRange(deleteData);
                        await _DbContext.SaveChangesAsync();
                        response.Value = true;
                    }

                    var updateData = _DbContext.SharedContact.AsEnumerable().Where(x => x.Type == TypeContact.Local).ExceptBy(request.List.Select(x => $"{x.NameCu}{x.AuthorityUrl}&{x.UserName}&{x.LastActive?.ToDateTime()}"), x => $"{x.NameCu}{x.AuthorityUrl}&{x.UserName}&{x.LastActive?.ToDateTime()}");

                    if (updateData?.Any() ?? false)
                    {
                        foreach (var item in updateData)
                        {
                            var first = request.List.FirstOrDefault(x => x.UserName == item.UserName);
                            if (first != null)
                            {
                                if (item.NameCu != first.NameCu)
                                {
                                    item.NameCu = first.NameCu;
                                }
                                if (item.AuthorityUrl != first.AuthorityUrl)
                                {
                                    _DbContext.Connects.Where(x => x.AuthorityUrl == item.AuthorityUrl && x.UserName == item.UserName).ExecuteUpdate(x => x.SetProperty(p => p.AuthorityUrl, first.AuthorityUrl));
                                }
                                if (item.LastActive != first.LastActive)
                                {
                                    item.LastActive = first.LastActive;
                                }
                            }
                        }
                        await _DbContext.SaveChangesAsync();
                        response.Value = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> AddRemoteContact(ContactInfoList request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                var newData = request.List.ExceptBy(_DbContext.SharedContact.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}");

                var updateData = _DbContext.SharedContact.AsEnumerable().IntersectBy(request.List.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}");

                var firstUrl = request.List.FirstOrDefault()?.AuthorityUrl;
                var deleteData = _DbContext.SharedContact.AsEnumerable().Where(x => x.Type == TypeContact.Remote && x.AuthorityUrl == firstUrl).ExceptBy(request.List.Select(x => $"{x.AuthorityUrl}&{x.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}");

                if (newData?.Any() ?? false)
                {
                    _DbContext.SharedContact.AddRange(newData);
                    await _DbContext.SaveChangesAsync();
                    response.Value = true;
                }

                if (updateData?.Any() ?? false)
                {
                    foreach (var item in updateData)
                    {
                        var lastActive = request.List.FirstOrDefault(x => x.AuthorityUrl == item.AuthorityUrl && x.UserName == item.UserName)?.LastActive;
                        if (lastActive != null)
                        {
                            item.LastActive = lastActive;
                        }
                    }
                    await _DbContext.SaveChangesAsync();
                    response.Value = true;
                }

                if (deleteData?.Any() ?? false)
                {
                    _DbContext.SharedContact.RemoveRange(deleteData);

                    foreach (var user in deleteData)
                    {
                        var deleteContactItems = _DbContext.Connects.Where(x => x.AuthorityUrl == user.AuthorityUrl && x.UserName == user.UserName);
                        if (deleteContactItems?.Any() ?? false)
                        {
                            _DbContext.Connects.RemoveRange(deleteContactItems);
                        }
                    }

                    await _DbContext.SaveChangesAsync();
                    response.Value = true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> SetCurrentRemoteContactForIp(CurrentRemoteCuArray request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                var deleteData = _DbContext.SharedContact.AsEnumerable().Where(x => x.Type == TypeContact.Remote).ExceptBy(request.List, x => x.AuthorityUrl);

                if (deleteData?.Any() ?? false)
                {
                    _DbContext.SharedContact.RemoveRange(deleteData);

                    foreach (var user in deleteData)
                    {
                        var deleteContactItems = _DbContext.Connects.Where(x => x.AuthorityUrl == user.AuthorityUrl && x.UserName == user.UserName);
                        if (deleteContactItems?.Any() ?? false)
                        {
                            _DbContext.Connects.RemoveRange(deleteContactItems);
                        }
                    }
                    await _DbContext.SaveChangesAsync();
                    response.Value = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> AddContactForUser(ContactForUser request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.Contact != null && request.UserKey != null)
                {
                    var idUser = GetIdUser(request.UserKey.AuthorityUrl, request.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var find = _DbContext.ContactForUser.FirstOrDefault(x => x.UserIdentityId == idUser && x.AuthorityUrl == request.Contact.AuthorityUrl && request.Contact.UserName == x.UserName);
                        if (find == null && !_DbContext.SharedContact.Any(x => x.AuthorityUrl == request.Contact.AuthorityUrl && request.Contact.UserName == x.UserName))
                        {
                            request.Contact.UserIdentityId = idUser;
                            _DbContext.ContactForUser.AddRange(request.Contact);
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override Task<ContactInfoList> GetAllContactForUser(UserIdentity request, ServerCallContext context)
        {
            ContactInfoList response = new();
            try
            {
                var idUser = GetIdUser(request.AuthorityUrl, request.UserName);

                if (idUser > 0)
                {
                    response.List.AddRange(_DbContext.ContactForUser.OrderBy(x => x.AuthorityUrl).ThenBy(x => x.UserName).Where(x => x.UserIdentityId == idUser).Select(x => new ContactInfo()
                    {
                        AuthorityUrl = x.AuthorityUrl,
                        LastActive = x.LastActive,
                        UserName = x.UserName,
                        NameCu = x.NameCu,
                        StaffId = x.StaffId,
                        Type = TypeContact.Manual
                    }));

                    response.List.AddRange(_DbContext.SharedContact.OrderBy(x => x.Type).ThenBy(x => x.AuthorityUrl).ThenBy(x => x.UserName));
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override Task<ContactInfoList> GetLocalContact(Empty request, ServerCallContext context)
        {
            ContactInfoList response = new();
            try
            {
                response.List.AddRange(_DbContext.SharedContact.OrderBy(x => x.UserName).Where(x => x.Type == TypeContact.Local));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override Task<ContactInfoList> GetRemoteContactForUrl(StringValue request, ServerCallContext context)
        {
            ContactInfoList response = new();
            try
            {
                if (!string.IsNullOrEmpty(request.Value))
                {
                    response.List.AddRange(_DbContext.SharedContact.OrderBy(x => x.UserName).Where(x => x.AuthorityUrl == request.Value && x.Type == TypeContact.Remote));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override Task<StringValue> FindCuName(StringValue request, ServerCallContext context)
        {
            StringValue response = new();
            try
            {
                response.Value = _DbContext.SharedContact.FirstOrDefault(x => x.AuthorityUrl == request.Value)?.NameCu;
                if (string.IsNullOrEmpty(response.Value))
                {
                    response.Value = _DbContext.ContactForUser.FirstOrDefault(x => x.AuthorityUrl == request.Value)?.NameCu;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return Task.FromResult(response);
        }

        public override async Task<BoolValue> UpdateLastActiveContact(ContactInfo request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                response = await UpdateLastActiveLocalContact(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        async Task<BoolValue> UpdateLastActiveLocalContact(ContactInfo request)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (SetUpdateActiveForContact != null && SetUpdateActiveForContact.AuthorityUrl == request.AuthorityUrl && SetUpdateActiveForContact.UserName == request.UserName)
                {
                    response.Value = true;
                }
                else
                {
                    SetUpdateActiveForContact = new(request);
                    var sharedContact = _DbContext.SharedContact.FirstOrDefault(x => x.AuthorityUrl == request.AuthorityUrl && x.UserName == request.UserName);
                    if (sharedContact != null)
                    {
                        sharedContact.LastActive = request.LastActive;
                        _DbContext.SharedContact.Update(sharedContact);
                        await _DbContext.SaveChangesAsync();
                        response.Value = true;
                    }
                    else
                    {
                        var manualContact = _DbContext.ContactForUser.Where(x => x.AuthorityUrl == request.AuthorityUrl && x.UserName == request.UserName);
                        if (manualContact != null)
                        {
                            manualContact.ExecuteUpdate(x => x.SetProperty(p => p.LastActive, request.LastActive));
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            SetUpdateActiveForContact = null;
            return response;
        }


        public override async Task<BoolValue> DeleteContactForUser(DeleteContactKey request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.UserKey != null && request.ContactKey != null)
                {
                    var idUser = GetIdUser(request.UserKey.AuthorityUrl, request.UserKey.UserName);

                    var user = _DbContext.ContactForUser.FirstOrDefault(x => x.UserIdentityId == idUser && x.AuthorityUrl == request.ContactKey.AuthorityUrl && x.UserName == request.ContactKey.UserName);
                    if (user != null)
                    {
                        _DbContext.ContactForUser.Remove(user);

                        var deleteContactItems = _DbContext.Connects.Where(x => x.AuthorityUrl == user.AuthorityUrl && x.UserName == user.UserName);
                        if (deleteContactItems?.Any() ?? false)
                        {
                            _DbContext.Connects.RemoveRange(deleteContactItems);
                        }
                        await _DbContext.SaveChangesAsync();
                        response.Value = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> UpdateNameChat(NewNameForChat request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.Key?.UserKey != null)
                {
                    var idUser = GetIdUser(request.Key.UserKey.AuthorityUrl, request.Key.UserKey.UserName);

                    if (idUser > 0)
                    {
                        var chat = _DbContext.Chats.FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.Key.ChatKey);

                        if (chat != null)
                        {
                            chat.NameRoom = request.NewName;
                            _DbContext.Chats.Update(chat);
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> DeleteChat(UserKeyAndChatKey request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.UserKey != null)
                {
                    var idUser = GetIdUser(request.UserKey.AuthorityUrl, request.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var chat = _DbContext.Chats.FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.ChatKey);

                        if (chat != null)
                        {
                            _DbContext.Chats.Remove(chat);
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                            RemoveFilesForChat(request.UserKey.UserName, request.ChatKey, chat.NameRoom);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        void RemoveFilesForChat(string userName, string keyChatRoom, string chatName)
        {
            try
            {
                Regex regexUserName = new(@"[^\w]");
                string dir = Path.Combine(DirectoryTmp, regexUserName.Replace(Convert.ToBase64String(Encoding.UTF8.GetBytes(userName)), ""), keyChatRoom);
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch
            {
                _logger.LogError(@"Ошибка удаления архива для пользователя {userName}, комната {chat}", userName, chatName);
            }
        }


        public override async Task<BoolValue> DeleteConnectsInChat(ConnectList request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.Key?.UserKey != null)
                {
                    var idUser = GetIdUser(request.Key.UserKey.AuthorityUrl, request.Key.UserKey.UserName);

                    if (idUser > 0)
                    {
                        var chat = _DbContext.Chats.Include(x => x.Items).FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.Key.ChatKey);

                        if (chat != null)
                        {
                            chat.Items.RemoveAll(x => request.Items.Any(r => r.AuthorityUrl == x.AuthorityUrl && r.UserName == x.UserName));
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public override async Task<BoolValue> AddConnectsInChat(ConnectList request, ServerCallContext context)
        {
            BoolValue response = new() { Value = false };
            try
            {
                if (request.Key?.UserKey != null)
                {
                    var idUser = GetIdUser(request.Key.UserKey.AuthorityUrl, request.Key.UserKey.UserName);
                    if (idUser > 0)
                    {
                        var chat = _DbContext.Chats.Include(x => x.Items).FirstOrDefault(x => x.UserIdentityId == idUser && x.Key == request.Key.ChatKey);

                        if (chat != null)
                        {
                            var listForAdd = request.Items.ExceptBy(chat.Items.Select(c => $"{c.AuthorityUrl}&{c.UserName}"), x => $"{x.AuthorityUrl}&{x.UserName}");
                            chat.Items.AddRange(listForAdd);
                            await _DbContext.SaveChangesAsync();
                            response.Value = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return response;
        }

        public void Dispose()
        {
            _DbContext.Dispose();
        }
    }
}
