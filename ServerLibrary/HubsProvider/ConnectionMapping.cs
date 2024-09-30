using SensorM.GsoCommon.ServerLibrary.Models;
using SharedLibrary.Utilities;

namespace ServerLibrary.HubsProvider
{
    public class ConnectionMapping
    {
        private readonly List<ConnectionsForHub> _connections = new();

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(string authorityUrl, string contextId, Guid guidUi, string userName)
        {
            lock (_connections)
            {
                var conn = _connections.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl));

                if (conn == null)
                {
                    conn = new(authorityUrl);
                    _connections.Add(conn);
                }

                var firstConnect = conn.ConnectionsItem.FirstOrDefault(x => x.IdUiConnect == guidUi);
                if (firstConnect != null)
                {
                    firstConnect.ContextId = contextId;
                }
                else
                {
                    firstConnect = new(contextId, guidUi, userName);
                    conn.ConnectionsItem.Add(firstConnect);
                }
            }
        }

        public MyConnectInfo? GetMyConnect(string contextId)
        {
            lock (_connections)
            {
                if (_connections.Any(x => x.ConnectionsItem.Any(x => x.ContextId == contextId)))
                {
                    var first = _connections.First(x => x.ConnectionsItem.Any(x => x.ContextId == contextId));

                    var userName = first.ConnectionsItem.FirstOrDefault(x => x.ContextId == contextId)?.UserName;
                    if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(first.AuthorityUrl))
                    {
                        return new MyConnectInfo(first.AuthorityUrl, userName);
                    }
                }
            }
            return null;
        }

        public IEnumerable<string> GetContextIdForLogin(string? authorityUrl, string? userName)
        {
            if (authorityUrl != null && !string.IsNullOrEmpty(userName))
            {
                if (_connections.Any(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl)))
                {
                    return _connections.First(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl)).ConnectionsItem.Where(x => x.UserName == userName).Select(x => x.ContextId);
                }
            }
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetContextIdForHost(string[]? authorityUrl)
        {
            if (authorityUrl != null && authorityUrl.Length > 0)
            {
                var listContextId = _connections.Where(x => authorityUrl.Select(u => IpAddressUtilities.GetHost(u)).Contains(IpAddressUtilities.GetHost(x.AuthorityUrl)));

                if (listContextId?.Any() ?? false)
                {
                    return listContextId.SelectMany(x => x.ConnectionsItem).Select(x => x.ContextId).Distinct();
                }
            }
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetAllConnectedUser()
        {
            return _connections.SelectMany(x => x.ConnectionsItem).Select(x => x.UserName).Distinct();
        }

        public IEnumerable<string> GetContextIdForGuid(Guid? idUiConnect)
        {
            if (idUiConnect != null)
            {
                if (_connections.Any(x => x.ConnectionsItem.Any(x => x.IdUiConnect.Equals(idUiConnect))))
                {
                    return _connections.SelectMany(x => x.ConnectionsItem).Where(x => x.IdUiConnect.Equals(idUiConnect)).Select(x => x.ContextId);
                }
            }
            else
                throw new ArgumentNullException(nameof(idUiConnect));
            return Enumerable.Empty<string>();
        }

        public Guid? GetGuidForContextId(string contextId)
        {
            if (_connections.Any(x => x.ConnectionsItem.Any(x => x.ContextId == contextId)))
            {
                return _connections.SelectMany(x => x.ConnectionsItem).First(x => x.ContextId == contextId).IdUiConnect;
            }
            return null;
        }

        public bool AnyContextId(string? contextId)
        {
            return _connections.Any(x => x.ConnectionsItem.Any(x => x.ContextId == contextId));

        }

        public bool AnyGuid(Guid? guidUi)
        {
            if (guidUi == null)
                return false;

            return _connections.Any(x => x.ConnectionsItem.Any(x => x.IdUiConnect.Equals(guidUi)));
        }

        public void RemoveForConnectId(string contextId)
        {
            lock (_connections)
            {
                var conn = _connections.FirstOrDefault(x => x.ConnectionsItem.Any(i => i.ContextId == contextId));

                if (conn == null)
                {
                    return;
                }

                conn.ConnectionsItem.RemoveAll(x => x.ContextId == contextId);

                if (conn.ConnectionsItem.Count == 0)
                {
                    _connections.Remove(conn);
                }
            }
        }

        public void Remove(string authorityUrl, Guid guidUi)
        {
            lock (_connections)
            {
                var conn = _connections.FirstOrDefault(x => IpAddressUtilities.CompareForAuthority(x.AuthorityUrl, authorityUrl));

                if (conn == null)
                {
                    return;
                }

                conn.ConnectionsItem.RemoveAll(x => x.IdUiConnect == guidUi);

                if (conn.ConnectionsItem.Count == 0)
                {
                    _connections.Remove(conn);
                }
            }
        }
    }
}
