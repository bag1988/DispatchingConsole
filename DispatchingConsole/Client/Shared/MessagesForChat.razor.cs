using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SharedLibrary.Models;
using SharedLibrary.Utilities;

namespace DispatchingConsole.Client.Shared
{
    partial class MessagesForChat
    {
        [Parameter]
        public List<ChatMessage>? Messages { get; set; }

        List<ChatMessage>? CacheMessages { get; set; }

        string? UserName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            UserName = await _User.GetName();
        }

        bool _shouldRender = false;

        protected override bool ShouldRender() => _shouldRender;

        protected override Task OnParametersSetAsync()
        {
            _shouldRender = false;
            List<ChatMessage> newData = new();
            if (CacheMessages == null)
                CacheMessages = new();

            newData = Messages?.Except(CacheMessages).ToList() ?? new();
            if (newData.Any())
            {
                var isScrol = !CacheMessages.Any();
                CacheMessages.AddRange(newData);
                _shouldRender = true;
                if (isScrol)
                    _ = _webRtc.ScrollToElement();
                _ = _webRtc.SetObjObserver();
            }
            if (Messages == null || Messages.Count == 0)
            {
                CacheMessages = new();
                _shouldRender = true;
            }

            return base.OnParametersSetAsync();
        }

        bool NextUserEqual(ChatMessage item)
        {
            var index = Messages?.IndexOf(item) ?? 0;

            if (index > -1 && index < Messages?.Count)
            {
                if (index + 1 == Messages.Count)
                {
                    return false;
                }
                var next = Messages.ElementAtOrDefault(index + 1);
                return (next.AuthorityUrl == item.AuthorityUrl && next.UserName == item.UserName);
            }
            return false;
        }
               
        string GetNamePu(ChatMessage? item)
        {
            return _webRtc.ContactList.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, item?.AuthorityUrl) && x.UserName == item?.UserName)?.Name ?? IpAddressUtilities.GetHost(item?.AuthorityUrl);
        }
    }
}
