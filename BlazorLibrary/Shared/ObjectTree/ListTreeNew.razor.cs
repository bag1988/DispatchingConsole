﻿using System.Linq;
using System.Net.Http.Json;
using BlazorLibrary.Models;
using SMSSGsoProto.V1;
using UUZSDataProto.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary;
using SMDataServiceProto.V1;
using System.ComponentModel;
using SharedLibrary.Interfaces;
using AsoDataProto.V1;
using BlazorLibrary.Helpers;

namespace BlazorLibrary.Shared.ObjectTree
{
    partial class ListTreeNew : IAsyncDisposable, IPubSubMethod
    {
        [Parameter]
        public RenderFragment? AddBackButons { get; set; }

        [Parameter]
        public RenderFragment? AddTopContent { get; set; }

        [Parameter]
        public RenderFragment<List<Models.ItemTree>?>? AddNextButons { get; set; }

        [Parameter]
        public List<Objects>? Folders { get; set; }

        [Parameter]
        public List<Models.ItemTree>? SelectItems { get; set; }

        [Parameter]
        public int? ListId { get; set; }

        [Parameter]
        public bool IsCreateList { get; set; } = true;//создаем список, или сценарий               

        [Parameter]
        public bool IsReadOnly { get; set; } = false;

        int SystemId => ParseUrlSegments.GetSystemId(MyNavigationManager.Uri);

        List<Google.Protobuf.WellKnownTypes.Any>? SelectList { get; set; }

        List<Google.Protobuf.WellKnownTypes.Any>? SelectListSelected { get; set; }

        private string WarningDelete = "";

        private bool IsLoadAll = false;

        private Objects? LoadChildForFolder { get; set; }

        private int? Abon = null;

        private bool IsDeleteAbon { get; set; } = false;

        private bool OnDelete { get; set; } = false;

        private List<string>? ListObj = new();

        private string SelectTitle = "";
        private string ForSelectTitle = "";

        private bool AutoAdd { get; set; } = false;

        public class CacheItem
        {
            public Objects Key { get; init; }
            public List<CGetSitItemInfo>? Childs { get; set; }

            public CacheItem(Objects key, List<CGetSitItemInfo>? childs = null)
            {
                Key = key;
                Childs = childs;
            }
        }

        public List<CacheItem> Caches { get; set; } = new();

        protected override void OnInitialized()
        {
            if (IsCreateList)
            {
                if (SystemId == SubsystemType.SUBSYST_ASO)
                {
                    SelectTitle = GsoRep["IDS_STRING_AB_SELECTED_IN_LIST"];
                    ForSelectTitle = GsoRep["IDS_STRING_AB_LIST_CAPTION"];
                }
                else if (SystemId == SubsystemType.SUBSYST_SZS)
                {
                    SelectTitle = GsoRep["IDS_STRING_DEVICE_SELECTED_IN_LIST"];
                    ForSelectTitle = GsoRep["IDS_STRING_DEVICE_LIST_CAPTION"];
                }
            }
            else
            {
                if (SystemId == SubsystemType.SUBSYST_ASO)
                {
                    SelectTitle = GsoRep["IDS_STRING_AB_SELECTED"];
                    ForSelectTitle = GsoRep["IDS_STRING_LIST_AB_COMMON"];
                }
                else if (SystemId == SubsystemType.SUBSYST_GSO_STAFF)
                {
                    SelectTitle = StaffRep["ListObjNotify"];
                    ForSelectTitle = StaffRep["ListObjManagement"];
                }
                else if (SystemId == SubsystemType.SUBSYST_SZS)
                {
                    SelectTitle = UUZSRep["DEVICE_SELECT_NOTIFY"];
                    ForSelectTitle = UUZSRep["GENERAL_LIST_DEVICE"];
                }
            }
            _ = _HubContext.SubscribeAndStartAsync(this, typeof(IPubSubMethod));
        }

        [Description(DaprMessage.PubSubName)]
        public Task Fire_InsertDeleteAbonent(byte[] AbonentItemByte)
        {
            try
            {
                if (SystemId == SubsystemType.SUBSYST_ASO)
                {
                    var newItem = AbonentItem.Parser.ParseFrom(AbonentItemByte);
                    if (newItem?.IDAb > 0 && newItem.StaffID > 0)
                    {
                        if (string.IsNullOrEmpty(newItem.AbName))//удаляем абонента
                        {
                            SelectList?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && obj.AsoAbonStaffID == newItem.StaffID && obj.AsoAbonID == newItem.IDAb);

                            foreach (var item in Caches.Where(x => x.Key.Type == ListType.MAN))
                            {
                                item.Childs?.RemoveAll(x => x.AsoAbonStaffID == newItem.StaffID && x.AsoAbonID == newItem.IDAb);
                            }
                            if (SelectItems != null)
                            {
                                foreach (var item in SelectItems.Where(x => x.Key.Type == ListType.MAN))
                                {
                                    item.Child.RemoveAll(x => x.AsoAbonStaffID == newItem.StaffID && x.AsoAbonID == newItem.IDAb);
                                }

                                SelectItems.RemoveAll(x => x.Child.Count == 0);

                                SelectListSelected?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && obj.AsoAbonStaffID == newItem.StaffID && obj.AsoAbonID == newItem.IDAb);
                            }
                        }
                        else //добавляем абонента
                        {
                            var firstSymbols = newItem.AbName.Substring(0, 1);

                            if (Folders != null && Folders.Any(x => x.Name == firstSymbols && x.Type == ListType.MAN))
                            {
                                lock (Folders)
                                {
                                    var key = Folders.FirstOrDefault(x => x.Name == firstSymbols && x.Type == ListType.MAN);

                                    if (key != null)
                                    {
                                        if (Caches.Any(x => x.Key == key))
                                        {
                                            var childs = Caches.First(x => x.Key == key).Childs;
                                            if (childs?.Count > 0)
                                            {
                                                if (!childs.Any(c => c.AsoAbonStaffID == newItem.StaffID && c.AsoAbonID == newItem.IDAb && c.Name == newItem.AbName))
                                                {
                                                    childs.Add(new CGetSitItemInfo()
                                                    {
                                                        Name = newItem.AbName,
                                                        AsoAbonID = newItem.IDAb,
                                                        AsoAbonStaffID = newItem.StaffID
                                                    });
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return Task.CompletedTask;
        }

        [Description(DaprMessage.PubSubName)]
        public async Task Fire_InsertDeleteTermDevice(long Value)
        {
            if (SystemId == SubsystemType.SUBSYST_SZS)
            {
                if (Folders != null)
                {
                    if (Folders.Any(x => x.Type == ListType.MEN))
                    {
                        foreach (var item in Folders.Where(x => x.Type == ListType.MEN))
                        {
                            if (Caches.Any(x => x.Key == item))
                            {
                                var childs = Caches.First(x => x.Key == item).Childs;
                                if (childs?.Count > 0)
                                {
                                    var newData = await GetListForDev(new GetItemRequest() { ObjID = new OBJ_ID() { StaffID = item.OBJID.StaffID }, NObjType = item.OBJID.ObjID });

                                    if (newData != null)
                                    {
                                        var forRemove = childs.Except(newData).ToList();
                                        var forAdd = newData.Except(childs).ToList();
                                        if (forRemove?.Count > 0)
                                        {
                                            childs.RemoveAll(x => forRemove.Contains(x));

                                            foreach (var r in forRemove)
                                            {
                                                SelectList?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && obj.SZSGroupID == r.SZSGroupID && obj.SZSGroupStaffID == r.SZSGroupStaffID && r.SZSDevID == obj.SZSDevID && r.DevType == obj.DevType);
                                                if (SelectItems != null)
                                                {
                                                    foreach (var s in SelectItems.Where(x => x.Key.Type == ListType.MEN))
                                                    {
                                                        s.Child?.RemoveAll(x => x.SZSGroupID == r.SZSGroupID && x.SZSGroupStaffID == r.SZSGroupStaffID && r.SZSDevID == x.SZSDevID && r.DevType == x.DevType);
                                                    }
                                                    SelectItems.RemoveAll(x => x.Child.Count == 0);
                                                    SelectListSelected?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && obj.SZSGroupID == r.SZSGroupID && obj.SZSGroupStaffID == r.SZSGroupStaffID && r.SZSDevID == obj.SZSDevID && r.DevType == obj.DevType);
                                                }
                                            }
                                        }

                                        if (forAdd?.Count > 0)
                                        {
                                            childs.AddRange(forAdd);
                                        }

                                        StateHasChanged();
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        private IEnumerable<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? GetFolders
        {
            get
            {
                List<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? response = null;
                try
                {
                    if (Folders != null)
                    {
                        lock (Folders)
                        {
                            var forRemove = Caches.ExceptBy(Folders.Select(x => x), x => x.Key).Select(x => x.Key);

                            var forAdd = Folders.ExceptBy(Caches.Select(x => x.Key), x => x);

                            if (forRemove?.Any() ?? false)
                            {
                                if (forRemove.Contains(LoadChildForFolder))
                                    LoadChildForFolder = null;

                                SelectItems?.RemoveAll(x => forRemove.Contains(x.Key));

                                var forRemoveItem = Caches.Where(x => forRemove.Contains(x.Key)).SelectMany(x => x.Childs ?? new()).ToList();

                                Caches.RemoveAll(x => forRemove.Contains(x.Key));
                                SelectList?.RemoveAll(x => x.Is(Objects.Descriptor) && x.TryUnpack<Objects>(out var obj) && forRemove.Contains(obj));

                                SelectListSelected?.RemoveAll(x => x.Is(Objects.Descriptor) && x.TryUnpack<Objects>(out var obj) && forRemove.Contains(obj));

                                if (forRemoveItem.Count > 0)
                                {
                                    SelectList?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && forRemoveItem.Contains(obj));
                                    SelectListSelected?.RemoveAll(x => x.Is(CGetSitItemInfo.Descriptor) && x.TryUnpack<CGetSitItemInfo>(out var obj) && forRemoveItem.Contains(obj));
                                }
                            }

                            if (forAdd?.Any() ?? false)
                            {
                                Caches.AddRange(forAdd.Select(x => new CacheItem(x)));
                            }
                        }

                        response = new();
                        response.AddRange(Folders.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x), GetChildItems)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                return response;
            }
        }

        IEnumerable<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? GetChildItems(Google.Protobuf.WellKnownTypes.Any item)
        {
            var b = item.TryUnpack<Objects>(out var obj);

            if (b)
            {
                if (SystemId == SubsystemType.SUBSYST_SZS && obj.Type == ListType.LIST)
                {
                    var elems = GetChildItems(obj)?.ToList();
                    if (elems?.Count > 0)
                    {
                        var r = Folders?.Where(x => x.Type == ListType.MEN && elems.Any(e => e.DevType == x.OBJID?.ObjID)).Select(x => new Objects(obj) { Name = x.Name, Type = ListType.MEN, Comm = x.OBJID.ObjID.ToString() });
                        return r?.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x), GetChildItems));
                    }
                    return new List<ChildItems<Google.Protobuf.WellKnownTypes.Any>>();
                }
                else
                {
                    return GetChildItems(obj)?.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x)));
                }
            }
            return null;
        }

        IEnumerable<CGetSitItemInfo>? GetChildItems(Objects? key)
        {
            if (key == null)
                return null;

            var keySzs = Folders?.FirstOrDefault(x => x.Type == ListType.LIST && key.OBJID.Equals(x.OBJID));
            int szsType = 0;
            if (SystemId == SubsystemType.SUBSYST_SZS && !string.IsNullOrEmpty(key.Comm) && keySzs != null)
            {
                int.TryParse(key.Comm, out szsType);
                if (szsType > 0)
                    key = keySzs;
                else
                    szsType = 0;
            }

            if (!Caches.Any(x => x.Key.Equals(key)))
                return null;

            var f = Caches.FirstOrDefault(x => x.Key.Equals(key))?.Childs;

            var s = SelectItems?.SelectMany(x => x.Child).ToList();

            var ex = f?.Where(x => !s?.Any(r => r.AsoAbonID.Equals(x.AsoAbonID) && r.SZSDevID.Equals(x.SZSDevID) && r.SZSGroupID.Equals(x.SZSGroupID)) ?? true).ToList();

            if (SystemId == SubsystemType.SUBSYST_SZS && szsType > 0)
                return ex?.Where(x => x.DevType == szsType);
            return ex;
        }

        private IEnumerable<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? GetSelectFolder
        {
            get
            {
                List<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? response = null;
                if (SelectItems != null)
                {
                    response =
                    [
                        .. SelectItems.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x.Key), GetChildSelectFolder)),
                    ];
                }
                return response;
            }
        }

        private IEnumerable<ChildItems<Google.Protobuf.WellKnownTypes.Any>>? GetChildSelectFolder(Google.Protobuf.WellKnownTypes.Any item)
        {
            var b = item.TryUnpack<Objects>(out var obj);

            if (b)
            {
                if (SystemId == SubsystemType.SUBSYST_SZS && obj.Type == ListType.LIST)
                {
                    var elems = GetChildSelectFolder(obj)?.ToList();
                    if (elems?.Count > 0)
                    {
                        var r = Folders?.Where(x => x.Type == ListType.MEN && elems.Any(e => e.DevType == x.OBJID?.ObjID)).Select(x => new Objects(obj) { Name = x.Name, Type = ListType.MEN, Comm = x.OBJID.ObjID.ToString() });
                        return r?.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x), GetChildSelectFolder));
                    }
                    return new List<ChildItems<Google.Protobuf.WellKnownTypes.Any>>();
                }
                else
                {
                    return GetChildSelectFolder(obj)?.Select(x => new ChildItems<Google.Protobuf.WellKnownTypes.Any>(Google.Protobuf.WellKnownTypes.Any.Pack(x)));
                }
            }
            return null;
        }

        IEnumerable<CGetSitItemInfo>? GetChildSelectFolder(Objects? key)
        {
            if (key == null)
                return null;

            var keySzs = SelectItems?.FirstOrDefault(x => x.Key.Type == ListType.LIST && key.OBJID.Equals(x.Key.OBJID))?.Key;
            int szsType = 0;
            if (SystemId == SubsystemType.SUBSYST_SZS && !string.IsNullOrEmpty(key.Comm) && keySzs != null)
            {
                int.TryParse(key.Comm, out szsType);
                if (szsType > 0)
                    key = keySzs;
                else
                    szsType = 0;
            }

            if (SystemId == SubsystemType.SUBSYST_SZS && szsType > 0)
                return SelectItems?.FirstOrDefault(x => x.Key.Equals(key))?.Child?.Where(x => x.DevType == szsType);
            return SelectItems?.FirstOrDefault(x => x.Key.Equals(key))?.Child;
        }

        private string GetIconName(int? id)
        {
            string response = "folder text-warning";
            switch (id)
            {
                case ListType.MAN:
                case ListType.Aso: response = "people text-warning"; break;
                case ListType.Szs: response = "bullhorn"; break;
                case ListType.LIST: response = "list"; break;
                case ListType.Staff: response = "monitor"; break;
            }
            return response;
        }

        private string GetChildIcon(CGetSitItemInfo item)
        {
            string icon = "people text-warning";

            if (item.AsoAbonID > 0)
                return "person text-warning";
            if (item.SZSDevID > 0)
                return "volume-high text-warning";
            if (item.SZSGroupID > 0)
                return "bullhorn";
            return icon;
        }


        async Task SetCurrentItem(Google.Protobuf.WellKnownTypes.Any? item)
        {
            if (item != null)
            {
                if (item.Is(Objects.Descriptor) && item.TryUnpack<Objects>(out var obj))
                {
                    await OnLoadChildData(obj);
                }
            }
        }


        async Task SetSelectList(List<Google.Protobuf.WellKnownTypes.Any>? items)
        {
            if (items != null)
            {
                List<Google.Protobuf.WellKnownTypes.Any> newSelectList = new();

                var keys = items.Where(x => x.Is(Objects.Descriptor)).Select(x => x.Unpack<Objects>());

                var childs = items.Where(x => x.Is(CGetSitItemInfo.Descriptor)).Select(x => x.Unpack<CGetSitItemInfo>());

                foreach (var item in keys)
                {
                    await OnLoadChildData(item);
                    newSelectList.Add(Google.Protobuf.WellKnownTypes.Any.Pack(item));
                }

                var selectKeys = Caches.Where(x => keys?.Contains(x.Key) ?? false).SelectMany(x => x.Childs ?? []);

                newSelectList.AddRange(childs.Except(selectKeys ?? []).Select(x => Google.Protobuf.WellKnownTypes.Any.Pack(x)));

                SelectList = newSelectList;
                if (AutoAdd)
                {
                    AddSelected();
                }
            }
            else
            {
                SelectList = null;
            }
        }

        void SetSelectedList(List<Google.Protobuf.WellKnownTypes.Any>? items)
        {
            if (items != null)
            {
                List<Google.Protobuf.WellKnownTypes.Any> newSelectList = new();

                var keys = items.Where(x => x.Is(Objects.Descriptor)).Select(x => x.Unpack<Objects>());

                var childs = items.Where(x => x.Is(CGetSitItemInfo.Descriptor)).Select(x => x.Unpack<CGetSitItemInfo>());

                foreach (var item in keys)
                {
                    newSelectList.Add(Google.Protobuf.WellKnownTypes.Any.Pack(item));
                }

                var selectKeys = SelectItems?.Where(x => keys?.Contains(x.Key) ?? false).SelectMany(x => x.Child);

                newSelectList.AddRange(childs.Except(selectKeys ?? []).Select(x => Google.Protobuf.WellKnownTypes.Any.Pack(x)));

                SelectListSelected = newSelectList;
            }
            else
            {
                SelectListSelected = null;
            }
        }

        private void AddSelected()
        {
            if (LoadChildForFolder != null)
                return;
            Objects? newSelectFolder = null;
            CGetSitItemInfo? newSelectChild = null;
            bool isAddChild = false;
            if (SelectList != null && Folders != null)
            {
                if (SelectItems == null)
                    SelectItems = new();
                var folder = SelectList.Where(x => x.Is(Objects.Descriptor)).Select(x => x.Unpack<Objects>());

                if (folder.Count() > 0)
                {
                    newSelectFolder = folder.Last();// Folders.SkipWhile(x => !folder.Contains(x)).FirstOrDefault(x => !folder.Contains(x));
                    foreach (var f in folder)
                    {
                        var c = GetChildItems(f);
                        if (c != null)
                            InsertChild(f, c);
                    }
                }

                var items = SelectList.Where(x => x.Is(CGetSitItemInfo.Descriptor)).Select(x => x.Unpack<CGetSitItemInfo>());
                if (items.Count() > 0)
                {
                    var key = Caches.LastOrDefault(x => x.Childs?.Contains(items.Last()) ?? false);
                    newSelectChild = GetChildItems(key?.Key)?.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x));
                    newSelectFolder = key?.Key;
                    isAddChild = true;
                    if (key != null)
                        InsertChild(key.Key, items);
                }
            }

            SelectList = null;

            if (Folders?.Count > 0)
            {
                if (!isAddChild)
                {
                    if (newSelectFolder == null)
                    {
                        SelectList = new() { Google.Protobuf.WellKnownTypes.Any.Pack(Folders.Last()) };
                    }
                    else
                        SelectList = new() { Google.Protobuf.WellKnownTypes.Any.Pack(newSelectFolder) };
                }
                else
                {
                    if (newSelectChild == null)
                    {
                        if (newSelectFolder != null)
                        {
                            var elem = GetChildItems(newSelectFolder);
                            if (elem?.Count() > 0)
                            {
                                SelectList = new() { Google.Protobuf.WellKnownTypes.Any.Pack(elem.Last()) };
                            }
                            else
                            {
                                //var nextElem = Folders.SkipWhile(x => !newSelectFolder.Equals(x)).FirstOrDefault(x => !newSelectFolder.Equals(x)) ?? Folders.LastOrDefault(x => !newSelectFolder.Equals(x));
                                //if (nextElem != null)
                                SelectList = new() { Google.Protobuf.WellKnownTypes.Any.Pack(newSelectFolder) };
                            }
                        }
                    }
                    else
                        SelectList = new() { Google.Protobuf.WellKnownTypes.Any.Pack(newSelectChild) };
                }
            }
        }

        void InsertChild(Objects key, IEnumerable<CGetSitItemInfo>? items)
        {
            if (SelectItems == null)
                SelectItems = new();
            var insertItems = items?.Except(SelectItems.SelectMany(x => x.Child));
            if (insertItems?.Count() > 0)
            {
                var keySzs = Folders?.FirstOrDefault(x => x.Type == ListType.LIST && key.OBJID.Equals(x.OBJID));
                int szsType = 0;
                if (SystemId == SubsystemType.SUBSYST_SZS && !string.IsNullOrEmpty(key.Comm) && keySzs != null)
                {
                    int.TryParse(key.Comm, out szsType);
                    if (szsType > 0)
                        key = keySzs;
                }

                if (!SelectItems.Any(x => x.Key.Equals(key)))
                {
                    SelectItems.Add(new Models.ItemTree() { Key = key, Child = new(insertItems) });
                }
                else
                {
                    var elem = SelectItems.First(x => x.Key.Equals(key));
                    elem.Child.AddRange(insertItems);
                }
            }
        }

        private async Task AddAll()
        {
            if (Folders == null)
                return;

            IsLoadAll = true;
            foreach (var item in Folders)
            {
                await OnLoadChildData(item);
            }

            if (SelectItems == null)
                SelectItems = new();

            foreach (var item in Caches)
            {
                InsertChild(item.Key, item.Childs);
            }
            SelectList = null;
            IsLoadAll = false;
        }

        private async Task RemoveSelected(bool? IsDelete = false)
        {
            Objects? newSelectFolder = null;
            CGetSitItemInfo? newSelectChild = null;
            bool isDeleteChild = false;
            bool resultDelete = false;
            if (SelectListSelected != null && SelectItems != null)
            {
                List<CGetSitItemInfo> selectItem = new();
                var folder = SelectListSelected.Where(x => x.Is(Objects.Descriptor)).Select(x => x.Unpack<Objects>());

                if (folder.Count() > 0)
                {
                    foreach (var f in folder)
                    {
                        var childsForKey = SelectItems.FirstOrDefault(x => f.OBJID.Equals(x.Key.OBJID))?.Child;
                        if (childsForKey != null)
                        {
                            selectItem.AddRange(childsForKey);
                        }
                    }
                }

                var items = SelectListSelected.Where(x => x.Is(CGetSitItemInfo.Descriptor)).Select(x => x.Unpack<CGetSitItemInfo>());
                if (items.Count() > 0)
                {
                    var key = SelectItems.LastOrDefault(x => x.Child.Contains(items.Last()));
                    newSelectChild = key?.Child.SkipWhile(x => !items.Contains(x)).FirstOrDefault(x => !items.Contains(x)) ?? key?.Child.LastOrDefault(x => !items.Contains(x));
                    newSelectFolder = key?.Key;

                    isDeleteChild = true;
                    foreach (var child in items)
                    {
                        if (SelectItems.Any(x => x.Child.Contains(child)) && !selectItem.Contains(child))
                        {
                            selectItem.Add(child);
                        }
                    }
                }

                if (selectItem.Count > 0)
                {
                    resultDelete = await RemoveSelectedItem(selectItem, IsDelete);
                }

                if (Abon == null && resultDelete || (selectItem.Count == 0 && folder.Any()))
                {
                    newSelectFolder = SelectItems.SkipWhile(x => !folder.Contains(x.Key)).FirstOrDefault(x => !folder.Contains(x.Key))?.Key;
                    foreach (var f in folder)
                    {
                        var keySzs = SelectItems.FirstOrDefault(x => x.Key.Type == ListType.LIST && f.OBJID.Equals(x.Key.OBJID))?.Key;
                        if (SystemId == SubsystemType.SUBSYST_SZS && !string.IsNullOrEmpty(f.Comm) && keySzs != null)
                        {
                            int.TryParse(f.Comm, out var szsType);
                            if (szsType > 0)
                            {
                                SelectItems.FirstOrDefault(x => x.Key.Equals(keySzs))?.Child?.RemoveAll(x => x.DevType == szsType);
                                if (SelectItems.FirstOrDefault(x => x.Key.Equals(keySzs))?.Child?.Count == 0)
                                {
                                    SelectItems.RemoveAll(x => x.Key.Equals(keySzs));
                                }
                            }
                        }
                        else
                        {
                            SelectItems.RemoveAll(x => x.Key.Equals(f));
                        }
                    }

                    SelectListSelected = null;
                    if (SelectItems?.Count > 0)
                    {
                        if (!isDeleteChild)
                        {
                            if (newSelectFolder == null)
                            {
                                SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(SelectItems.Last().Key) };
                            }
                            else
                                SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(newSelectFolder) };
                        }
                        else
                        {
                            if (newSelectChild == null)
                            {
                                if (newSelectFolder != null)
                                {
                                    var elem = SelectItems.FirstOrDefault(x => x.Key.Equals(newSelectFolder));
                                    if (elem?.Child.Count > 0)
                                    {
                                        SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(elem.Child.Last()) };
                                    }
                                    else
                                    {
                                        var nextElem = SelectItems.SkipWhile(x => !newSelectFolder.Equals(x.Key)).FirstOrDefault(x => !newSelectFolder.Equals(x.Key))?.Key ?? SelectItems.LastOrDefault(x => !newSelectFolder.Equals(x.Key))?.Key;
                                        SelectItems.RemoveAll(x => x.Key.Equals(newSelectFolder));
                                        if (nextElem != null)
                                            SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(nextElem) };
                                    }
                                }
                                else if (SelectItems.LastOrDefault() != null)
                                {
                                    SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(SelectItems.Last().Key) };
                                }
                            }
                            else
                                SelectListSelected = new() { Google.Protobuf.WellKnownTypes.Any.Pack(newSelectChild) };
                        }
                    }
                }
            }
        }

        private async Task<bool> RemoveSelectedItem(List<CGetSitItemInfo>? selectItem, bool? IsDelete = false)
        {
            if (selectItem == null || SelectItems == null)
                return false;

            var c = SelectItems.FirstOrDefault(x => x.Child.Any(a => a.Equals(selectItem.First())));

            if (c != null)
            {
                if (SystemId == SubsystemType.SUBSYST_SZS)
                {
                    IsDelete = true;
                    WarningDelete = UUZSRep["IDS_STRING_ERROR_DELETE"];
                }
                else if (SystemId == SubsystemType.SUBSYST_ASO)
                {
                    WarningDelete = AsoRep["AbonInSit"];
                }

                //двойной клик, отображаем окно редактирования абонента
                if (SystemId == SubsystemType.SUBSYST_ASO && IsDelete == false && IsCreateList && selectItem.Count == 1)
                {
                    Abon = selectItem.First().AsoAbonID;
                }
                else
                {
                    if (((c.Key.Type == ListType.MAN && SystemId == SubsystemType.SUBSYST_ASO) || (c.Key.Type == ListType.MEN && SystemId == SubsystemType.SUBSYST_SZS)) && IsDelete == true && IsCreateList && ListId > 0)
                    {
                        if (OnDelete == false && selectItem != null)
                        {
                            ListObj = new();
                            CListItemInfo request = new()
                            {
                                ListID = ListId ?? 0,
                                ListStaffID = c.Key.OBJID.StaffID,
                                ListSubsystemID = c.Key.OBJID.SubsystemID,
                                AsoAbonID = selectItem.First().AsoAbonID,
                                AsoAbonStaffID = selectItem.First().AsoAbonStaffID,
                                SZSDevID = selectItem.First().SZSDevID,
                                SZSDevStaffID = selectItem.First().SZSDevStaffID
                            };

                            var result = await Http.PostAsJsonAsync("api/v1/GetLinkObjectsListItem", request);
                            if (result.IsSuccessStatusCode)
                            {
                                ListObj = await result.Content.ReadFromJsonAsync<List<string>>();
                            }

                            OnDelete = true;
                            IsDeleteAbon = true;
                            if (ListObj != null && ListObj.Count > 0)
                            {
                                return false;
                            }
                        }
                        else
                            OnDelete = true;

                        if (OnDelete == true)
                        {
                            ListObj = null;
                            OnDelete = false;
                            IsDeleteAbon = false;
                        }

                    }

                    if (selectItem != null)
                    {
                        foreach (var item in selectItem)
                        {
                            c.Child.Remove(item);
                        }
                        if (c.Child.Count == 0)
                        {
                            SelectItems.Remove(c);
                        }
                    }
                }
            }
            return true;
        }

        void RermoveSelectedAll()
        {
            SelectListSelected = null;
            SelectItems?.Clear();
        }

        private void CanselDelete()
        {
            OnDelete = false;
            ListObj = null;
            IsDeleteAbon = false;
        }


        async Task<List<CGetSitItemInfo>?> GetListForABC(IntAndString request)
        {
            try
            {
                var result = await Http.PostAsJsonAsync("api/v1/GetAbonentByABC", request);
                if (result.IsSuccessStatusCode)
                {
                    var r = await result.Content.ReadFromJsonAsync<List<AsoDataProto.V1.AbonentByABC>>() ?? new();

                    return r.Select(x => new CGetSitItemInfo()
                    {
                        Name = x.FIO,
                        AsoAbonID = x.IDAb,
                        AsoAbonStaffID = x.StaffID
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }


        async Task<List<CGetSitItemInfo>?> GetListForDev(GetItemRequest request)
        {
            try
            {
                List<CGetSitItemInfo> Childs = new();

                var result = await Http.PostAsJsonAsync("api/v1/GetItems_InOrder", request);
                if (result.IsSuccessStatusCode)
                {
                    var r = await result.Content.ReadFromJsonAsync<List<CLineGroupDev>>() ?? new();
                    Childs.AddRange(r.Select(x => new CGetSitItemInfo()
                    {
                        Name = x.DevName,
                        SZSDevID = x.DevID,
                        SZSDevStaffID = x.StaffID,
                        DevType = request.NObjType
                    }));
                }
                if (!IsCreateList)
                {
                    result = await Http.PostAsJsonAsync("api/v1/GetItems_IGroup", request);
                    if (result.IsSuccessStatusCode)
                    {
                        var r = await result.Content.ReadFromJsonAsync<List<CGroupInfoListOut>>() ?? new();
                        Childs.AddRange(r.Select(x => new CGetSitItemInfo()
                        {
                            Name = x.GroupName,
                            SZSGroupID = x.GroupID,
                            SZSGroupStaffID = request.ObjID.StaffID,
                            DevType = request.NObjType
                        }));
                    }
                }

                return Childs;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private async Task OnLoadChildData(Objects? item)
        {
            if (item == null) return;

            var cacheElem = Caches.FirstOrDefault(x => x.Key.Equals(item));

            if (cacheElem == null)
            {
                return;
            }

            if (cacheElem != null && (cacheElem.Childs == null || cacheElem.Childs.Count == 0))
            {
                LoadChildForFolder = item;
                switch (item?.Type)
                {
                    case ListType.LIST:
                    {
                        var result = await Http.PostAsJsonAsync("api/v1/GetListItems", item.OBJID);
                        if (result.IsSuccessStatusCode)
                        {
                            var r = await result.Content.ReadFromJsonAsync<List<Items>>() ?? new();
                            cacheElem.Childs = r.Select(x => new CGetSitItemInfo()
                            {
                                Name = x.Name,
                                DevType = x.DevType,
                                AsoAbonID = x.AsoAbon?.ObjID ?? 0,
                                AsoAbonStaffID = x.AsoAbon?.StaffID ?? 0,
                                SZSDevID = x.SZSDev?.ObjID ?? 0,
                                SZSDevStaffID = x.SZSDev?.StaffID ?? 0,
                                ListID = item.OBJID
                            }).ToList();
                        }
                    }; break;
                    case ListType.DEPARTMENT:
                    {
                        var result = await Http.PostAsJsonAsync("api/v1/GetDepAbonList", item.OBJID);
                        if (result.IsSuccessStatusCode)
                        {
                            var r = await result.Content.ReadFromJsonAsync<List<AsoDataProto.V1.AbonentByABC>>() ?? new();
                            cacheElem.Childs = r.Select(x => new CGetSitItemInfo()
                            {
                                Name = x.FIO,
                                AsoAbonID = x.IDAb,
                                AsoAbonStaffID = x.StaffID
                            }).ToList();
                        }
                    }; break;
                    case ListType.MAN:
                    {
                        cacheElem.Childs = await GetListForABC(new IntAndString() { Number = item.OBJID.StaffID, Str = item.Name });
                    }; break;
                    case ListType.MEN:
                    {
                        cacheElem.Childs = await GetListForDev(new GetItemRequest() { ObjID = new OBJ_ID() { StaffID = item.OBJID.StaffID }, NObjType = item.OBJID.ObjID });
                    }; break;
                }
            }
            LoadChildForFolder = null;
        }


        public ValueTask DisposeAsync()
        {
            return _HubContext.DisposeAsync();
        }

    }
}
