using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace Flow.Launcher.Plugin.VolumeController;

public class VolumeController : IPlugin {
    private readonly MMDeviceEnumerator _deviceEnumerator = new();

    private PluginInitContext _context;

    private float _currentVolume;
    private MMDevice _defaultPlaybackDevice;
    private bool _isMute;
    private AudioEndpointVolume _volume;

    public void Init(PluginInitContext context) {
        _context = context;
        _defaultPlaybackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _volume = _defaultPlaybackDevice.AudioEndpointVolume;
    }

    public List<Result> Query(Query query) {
        var commands = Commands(query);

        if (_defaultPlaybackDevice == null || _volume == null)
            return new List<Result> {
                new() {
                    Title = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_no_endpoints_found"),
                    IcoPath = _context.CurrentPluginMetadata.IcoPath
                }
            };

        return commands;
    }

    private List<Result> Commands(Query query) {
        var results = new List<Result>();

        var search = query.Search.Trim().ToLower();

        if (search.StartsWith("m")) {
            AddMuteResult(results);
        } else if (search.StartsWith("u")) {
            AddUnmuteResult(results);
        } else if (double.TryParse(query.Search, out var value)) {
            AddSetVolumeResult(results, value);
        } else {
            AddMuteResult(results);
            AddUnmuteResult(results);
            try {
                AddSetVolumeResult(results, double.Parse(query.Search));
            } catch {
                AddSetVolumeResult(results, -1);
            }

            if (query.Search.Length > 0 && !double.TryParse(query.Search, out _))
                results.RemoveAll(result => result.Title.Equals(_context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_title_set_volume")));
        }

        return results;
    }

    private void AddMuteResult(List<Result> results) {
        results.Add(new Result {
            Title = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_title_mute"),
            SubTitle = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_subtitle_mute"),
            Action = _ => {
                Mute();
                return true;
            },
            IcoPath = "Images/mute.png"
        });
    }

    private void AddUnmuteResult(List<Result> results) {
        results.Add(new Result {
            Title = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_title_unmute"),
            SubTitle = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_subtitle_mute"),
            Action = _ => {
                Unmute();
                return true;
            },
            IcoPath = "Images/unmute.png"
        });
    }

    private void AddSetVolumeResult(List<Result> results, double value) {
        var subTitle = value < 0 ? _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_subtitle_set_volume") : $"Set volume to {value}";

        results.Add(new Result {
            Title = _context.API.GetTranslation("flowlauncher_plugin_volume_controller_plugin_title_set_volume"),
            SubTitle = subTitle,
            Action = _ => {
                SetVolume(value);
                return true;
            },
            IcoPath = "Images/setvolme.png"
        });
    }

    public void SetVolume(double value) {
        value = Math.Max(0, Math.Min(100, value));
        _volume.MasterVolumeLevelScalar = (float)value / 100;
        _currentVolume = _volume.MasterVolumeLevelScalar;
    }

    public void Mute() {
        if (_isMute) return;

        _currentVolume = _volume.MasterVolumeLevelScalar;
        _volume.MasterVolumeLevelScalar = 0;

        _isMute = true;
    }

    public void Unmute() {
        _volume.MasterVolumeLevelScalar = _currentVolume;
        _isMute = false;
    }
}