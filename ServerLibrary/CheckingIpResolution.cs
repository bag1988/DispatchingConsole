using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ServerLibrary.Controllers;
using ServerLibrary.Extensions;
using SharedLibrary.Models;
using SharedLibrary.Utilities;
using SMDataServiceProto.V1;
using static Google.Rpc.Context.AttributeContext.Types;
using static SMDataServiceProto.V1.SMDataService;
using static SMSSGsoProto.V1.SMSSGso;

namespace SensorM.GsoCommon.ServerLibrary
{
    public class CheckingIpResolution
    {
        private readonly SMDataServiceClient _SMData;
        private readonly SMSSGsoClient _SMGso;
        private readonly ILogger<CheckingIpResolution> _logger;
        private readonly HttpContext? _HttpContext;
        public CheckingIpResolution(SMDataServiceClient sMData, SMSSGsoClient sMGso, ILogger<CheckingIpResolution> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _SMData = sMData;
            _SMGso = sMGso;
            _HttpContext = httpContextAccessor.HttpContext;
        }

        public async Task<bool> CheckWhiteList()
        {
            try
            {
                var connect = _HttpContext?.Connection;
                if (connect != null)
                {
                    var r = new Regex("(\\d{1,3}).(\\d{1,3}).(\\d{1,3}).(\\d{1,3})");

                    var localIp = r.Match(connect.LocalIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                    var remoteIp = r.Match(connect.RemoteIpAddress?.ToString() ?? "").Groups.Values.Select(x => x.Value);

                    var listIP = await _SMData.GetServerIPAddressesAsync(new Google.Protobuf.WellKnownTypes.Empty());

                    _logger.LogInformation(@"Вход по адресу {local}, с Ip-адреса {remote}, адреса в настройках {ipList}", connect.LocalIpAddress?.ToString(), connect.RemoteIpAddress?.ToString(), listIP?.Array);

                    bool isAccessBegin = false;
                    if ((listIP?.Array.Select(x => IpAddressUtilities.GetHost(x)).Contains(remoteIp.FirstOrDefault()) ?? false) || remoteIp.FirstOrDefault() == localIp.FirstOrDefault() || remoteIp.Skip(1).Take(3).SequenceEqual(localIp.Skip(1).Take(3)))
                    {
                        isAccessBegin = true;
                    }
                    else
                    {
                        var checkAccess = await _SMGso.IsContenIpInArmWhiteListByParamsAsync(new SMDataServiceProto.V1.String() { Value = remoteIp.FirstOrDefault() });
                        isAccessBegin = checkAccess?.Value ?? false;

                        _logger.LogInformation(@"Доступ для Ip-адреса {remote} {Access}", remoteIp.FirstOrDefault(), (isAccessBegin ? "разрешен" : "запрещен"));
                    }
                    return isAccessBegin;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CheckingIpResolution));
            }
            return false;
        }


        public async Task<string[]> GetAllowedIpForNotify()
        {
            try
            {
                var s = await _SMData.GetParamsAsync(new Google.Protobuf.WellKnownTypes.StringValue() { Value = nameof(ParamsSystem.NotifyStaffName) });

                if (!string.IsNullOrEmpty(s?.Value))
                {
                    return s.Value.Replace(" ", "").Split(";,|".ToCharArray());
                }
                else
                {
                    var ipList = await _SMData.GetServerIPAddressesAsync(new Google.Protobuf.WellKnownTypes.Empty()) ?? new();
                    if (!ipList.Array.Contains("localhost"))
                    {
                        ipList.Array.Add("localhost");
                    }
                    if (!ipList.Array.Contains("127.0.0.1"))
                    {
                        ipList.Array.Add("127.0.0.1");
                    }
                    if (!ipList.Array.Contains("0.0.0.0"))
                    {
                        ipList.Array.Add("0.0.0.0");
                    }
                    return ipList.Array.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CheckingIpResolution));
            }
            return Array.Empty<string>();
        }

        public async Task<string[]> GetAllowedIpForNotifyEx()
        {
            try
            {
                var result = await _SMGso.GetNotifyStaffIPListAsync(new Empty());

                if (result?.List?.Any() ?? false)
                {
                    var listAllowed = result?.List?.Where(x => x.Check == true);
                    if (listAllowed?.Any() ?? false)
                    {
                        return listAllowed.Select(x => x.Ip).ToArray();
                    }
                }
                else
                {
                    var ipList = await _SMData.GetServerIPAddressesAsync(new Google.Protobuf.WellKnownTypes.Empty()) ?? new();
                    if (!ipList.Array.Contains("localhost"))
                    {
                        ipList.Array.Add("localhost");
                    }
                    if (!ipList.Array.Contains("127.0.0.1"))
                    {
                        ipList.Array.Add("127.0.0.1");
                    }
                    if (!ipList.Array.Contains("0.0.0.0"))
                    {
                        ipList.Array.Add("0.0.0.0");
                    }
                    return ipList.Array.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.WriteLogError(ex, nameof(CheckingIpResolution));
            }
            return Array.Empty<string>();
        }
    }
}
