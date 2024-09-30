using System.Net.Http.Json;
using Google.Protobuf;
using Label.V1;
using Microsoft.AspNetCore.Components;
using SharedLibrary.Extensions;
using SMDataServiceProto.V1;
using Field = Label.V1.Field;

namespace BlazorLibrary.Shared.LabelsComponent
{
    partial class Labels
    {
        [Parameter]
        public OBJ_Key? IdForm { get; set; }

        public GetFieldList? keyList { get; set; }

        Field? SelectItem { get; set; }

        bool IsViewSpecification = false;

        bool IsPageLoad = true;

        protected override async Task OnInitializedAsync()
        {
            await GetKeyList();
            IsPageLoad = false;
        }

        IEnumerable<Field> GetProvider => keyList?.FieldList?.List.ToList() ?? new List<Field>();

        private async Task GetKeyList()
        {
            var result = await Http.PostAsJsonAsync("api/v1/GetLabelFieldAsoAbonent", IdForm);
            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(json))
                {
                    keyList = JsonParser.Default.Parse<GetFieldList>(json);
                }
            }

            keyList ??= new() { FieldList = new(), FieldHelpList = new() };

            if (keyList.FieldList?.List?.Count(x => !string.IsNullOrEmpty(x.ValueField)) > 0)
                IsViewSpecification = true;
        }


        void SetNameField(ChangeEventArgs e)
        {
            keyList ??= new() { FieldList = new(), FieldHelpList = new() };

            if ((!keyList.FieldList.List?.Any(x => x.NameField == e.Value?.ToString()) ?? true) && SelectItem != null)
            {
                SelectItem.NameField = e.Value?.ToString();
            }
        }

        void SetValueField(ChangeEventArgs e)
        {
            if (SelectItem == null || keyList == null)
                return;

            string nameField = SelectItem.NameField ?? "";

            var item = keyList.FieldList?.List?.FirstOrDefault(x => x.NameField == nameField);

            if (item != null)
            {
                item.ValueField = e.Value?.ToString();
            }
        }

        private IEnumerable<string> GetHelpValueList
        {
            get
            {
                List<string> valueList = new();

                if (!string.IsNullOrEmpty(SelectItem?.NameField))
                {
                    if (keyList?.FieldHelpList?.List?.Count > 0)
                    {
                        valueList = keyList.FieldHelpList.List.FirstOrDefault(x => x.NameField == SelectItem.NameField)?.HelpStringList?.List?.Select(x => x.Value)?.ToList() ?? new List<string>();
                    }
                }
                return valueList;
            }
        }


        IEnumerable<string>? GetFreeKey
        {
            get
            {
                List<string> freeKeyList = new();

                if (keyList?.FieldHelpList?.List?.Count > 0)
                {
                    freeKeyList.AddRange(keyList.FieldHelpList.List.Where(x => !GetProvider.Any(k => k.NameField == x.NameField)).Select(x => x.NameField));
                }
                return freeKeyList;
            }
        }


        void AddSpicification()
        {
            keyList ??= new() { FieldList = new() };

            var newItem = keyList.FieldList.List.FirstOrDefault(x => string.IsNullOrEmpty(x.NameField) || string.IsNullOrEmpty(x.ValueField));
            if (newItem == null)
            {
                newItem = new Field()
                {

                };
                keyList.FieldList.List.Add(newItem);
            }
            SelectItem = newItem;
        }


        void SetSelectItem(List<Field>? items)
        {
            if (SelectItem?.Equals(items?.LastOrDefault()) ?? false)
                return;
            SelectItem = items?.LastOrDefault();
            keyList?.FieldList?.List?.RemoveAll(x => string.IsNullOrEmpty(x.NameField) && string.IsNullOrEmpty(x.ValueField));
        }
    }
}
