using System.Net.Http.Json;
using BlazorLibrary.Models;
using BlazorLibrary.Shared.Audio;
using BlazorLibrary.Shared.Table;
using FiltersGSOProto.V1;
using GateServiceProto.V1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using SMDataServiceProto.V1;
using SMSSGsoProto.V1;
using SharedLibrary;
using BlazorLibrary.GlobalEnums;

namespace BlazorLibrary.Shared.ServiceMessageFolder
{
    partial class ViewServiceMessage : IAsyncDisposable
    {
        private List<ServiceMessage>? SelectedList = null;

        private bool ViewDopInfo = false;

        private AudioPlayerStream? player = default!;

        CUStartSitInfo? DopInfo
        {
            get
            {
                if (SelectedList?.LastOrDefault() != null)
                {
                    var byteStr = SelectedList.Last().Info;
                    if (byteStr != null && byteStr != ByteString.Empty)
                        return CUStartSitInfo.Parser.ParseFrom(byteStr);
                }
                return null;
            }
        }

        TableVirtualize<ServiceMessage>? table;

        protected override async Task OnInitializedAsync()
        {
            ThList = new Dictionary<int, string>
            {
                { 0, StartUIRep["IDS_TIME"] },
                { 1, StartUIRep["IDS_SIT_MSG_NAME"] }
            };

            HintItems.Add(new HintItem(nameof(FiltrModel.Messages), StartUIRep["IDS_SIT_MSG_NAME"], TypeHint.Select, null, FiltrOperationType.None, new VirtualizeProvider<Hint>(new GetItemRequest() { CountData = 100 }, LoadHelpMessage)));

            HintItems.Add(new HintItem(nameof(FiltrModel.DateRange), StartUIRep["IDS_TIME"], TypeHint.Date));

            await OnInitFiltr(RefreshTable, FiltrName.FiltrServiceMessage);
        }


        ItemsProvider<ServiceMessage> GetProvider => new ItemsProvider<ServiceMessage>(ThList, LoadChildList, request);

        private async ValueTask<IEnumerable<ServiceMessage>> LoadChildList(GetItemRequest req)
        {
            List<ServiceMessage>? newData = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetItems_IServiceMessages", req, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    try
                    {
                        var model = JsonParser.Default.Parse<ServiceMessageList>(json);

                        if (model != null)
                        {
                            newData = model.Array.ToList();
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"Error convert data to ServiceMessageList");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }            
            return newData ?? new();
        }

        private async Task RefreshTable()
        {
            if (table != null)
                await table.ResetData();
        }

        private async ValueTask<IEnumerable<Hint>> LoadHelpMessage(GetItemRequest req)
        {
            List<Hint>? newData = new();
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetMessageByServiceMessage", new StringValue() { Value = req.BstrFilter }, ComponentDetached);
                if (result.IsSuccessStatusCode)
                {
                    var response = await result.Content.ReadFromJsonAsync<List<IntAndString>>();

                    if (response?.Count > 0)
                    {
                        newData.AddRange(response.Select(x => new Hint(x.Str)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }            
            return newData ?? new();
        }

        private async Task DeleteSelect()
        {
            if (SelectedList == null)
                return;

            await DeleteServiceLogs(SelectedList);

            SelectedList = null;
        }

        private async Task DeleteServiceLogs(List<ServiceMessage>? request)
        {
            try
            {
                if (request == null)
                    return;

                var listIntID = request.Select(x => new IntID() { ID = x.Id });

                await Http.PostAsJsonAsync("api/v1/DeleteServiceLogs", listIntID, ComponentDetached);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }            
        }

        private async Task ClearServiceLogs()
        {
            try
            {
                await Http.PostAsync("api/v1/ClearServiceLogs", null, ComponentDetached);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }            
        }

        private void SetSelectList(List<ServiceMessage>? items)
        {
            SelectedList = items;
        }

        private async Task ViewInfo()
        {
            try
            {
                if (DopInfo != null)
                {
                    ViewDopInfo = true;
                    StateHasChanged();
                    await Task.Yield();

                    if (DopInfo.MsgID?.ObjID > 0 && player != null)
                    {
                        await player.SetUrlSound($"api/v1/GetSoundServer?MsgId={DopInfo.MsgID.ObjID}&Staff={DopInfo.MsgID.StaffID}&System={SubsystemType.SUBSYST_REMOTE_STAFF}&version={DateTime.Now.Second}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }           
        }
        void CloseViewInfo()
        {
            ViewDopInfo = false;
        }

        public async Task RefreshMe()
        {
            await CallRefreshData();
        }

        public ValueTask DisposeAsync()
        {
            DisposeToken();
            return ValueTask.CompletedTask;
        }
    }
}
