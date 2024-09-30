using System;
using System.Net.Http.Json;
using System.Web;
using BlazorLibrary.Helpers;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SharedLibrary.GlobalEnums;
using SharedLibrary.Models;
using SMDataServiceProto.V1;

namespace BlazorLibrary.Shared.Audio
{
    partial class AudioPlayerStream
    {
        [Parameter]
        public bool? IsAutoPlay { get; set; } = false;

        [Parameter]
        public string? TitleName { get; set; } = null;

        [Parameter]
        public string? BgColor { get; set; }

        private string? Blob = null;

        private bool IsSoundUrl { get; set; } = false;

        private bool IsLoadAudio = false;

        private ElementReference player;

        private Models.SndSetting SettingSound = new();

        string? _AppId = null;

        protected override async Task OnInitializedAsync()
        {
            await GetSndSettingExSound();
            _AppId = await _localStorage.GetAppIdAsync();
        }

        private async Task GetSndSettingExSound()
        {
            SettingSound = await _localStorage.GetSndSettingEx(SoundSettingsType.RepSoundSettingType) ?? new();
        }

        public async Task SetUrlSound(string url, bool? isDeleteOld = true)
        {
            IsLoadAudio = true;
            StateHasChanged();
            if (Blob != null && isDeleteOld == true)
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("RemoveBlob", Blob);
                }
                catch
                {
                    Console.WriteLine($"Error remove blob {Blob}");
                }
            }
            Blob = url;
            await CheckUrl();
        }


        string? GetUri
        {
            get
            {
                if (!string.IsNullOrEmpty(Blob))
                {
                    string response = Blob;
                    if (!string.IsNullOrEmpty(_AppId) && !Blob.Contains("blob:http"))
                    {
                        if (Blob.IndexOf("?") > 0)
                        {
                            response = $"{Blob}&{nameof(CookieName.AppId)}={_AppId}";
                        }
                        else
                        {
                            response = $"{Blob}?{nameof(CookieName.AppId)}={_AppId}";
                        }
                    }
                    return response;
                }

                return null;
            }
        }

        private async Task CheckUrl()
        {
            IsSoundUrl = false;
            long length = 0;

            length = await Http.GetLengthFileAsync(Blob);

            IsLoadAudio = false;
            StateHasChanged();

            if (length > 8000)
            {
                IsSoundUrl = true;
                StateHasChanged();
                await WaitSettingSound();                
            }           
        }


        private async Task WaitSettingSound()
        {           
            await JSRuntime.InvokeVoidAsync("InitAudioPlayer", player, SettingSound.Interfece, SettingSound.SndLevel);
        }
    }
}
