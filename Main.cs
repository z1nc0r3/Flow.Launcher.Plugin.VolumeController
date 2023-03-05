using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace Flow.Launcher.Plugin.VolumeController;

/// <summary>
/// Volume Controller class
/// </summary>
public class VolumeController : IPlugin {
    private readonly MMDeviceEnumerator _deviceEnumerator = new();

    private PluginInitContext _context;

    private float _currentVolume;
    private MMDevice _defaultPlaybackDevice;
    private bool _isMute;
    private AudioEndpointVolume _volume;

    /// <summary>
    /// Initialize the plugin when starting the Flow Launcher
    /// </summary>
    /// <param name="context"></param>
    public void Init(PluginInitContext context) {
        _context = context;
        _defaultPlaybackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _volume = _defaultPlaybackDevice.AudioEndpointVolume;
    }

    /// <summary>
    /// Query function which runs when user enter the action keyword
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public List<Result> Query(Query query) {
        _defaultPlaybackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        _volume = _defaultPlaybackDevice.AudioEndpointVolume;

        var commands = Commands(query);

        if (_defaultPlaybackDevice == null || _volume == null)
            return new List<Result> {
                new() {
                    Title = "No audio endpoints found",
                    IcoPath = "Images/volume_on.png"
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
                results.RemoveAll(result => result.Title.Equals("Set Volume"));
        }

        return results;
    }

    private void AddMuteResult(List<Result> results) {
        results.Add(new Result {
            Title = "Mute",
            SubTitle = "Mute the speaker",
            Action = _ => {
                Mute();
                return true;
            },
            IcoPath = "Images/volume_off.png"
        });
    }

    private void AddUnmuteResult(List<Result> results) {
        results.Add(new Result {
            Title = "Unmute",
            SubTitle = "Unmute the speaker",
            Action = _ => {
                Unmute();
                return true;
            },
            IcoPath = "Images/volume_on.png"
        });
    } 

    private void AddSetVolumeResult(List<Result> results, double value) {
        var subTitle = value < 0 ? "Enter custom value" : $"Set volume to {value}";

        results.Add(new Result {
            Title = "Set Volume",
            SubTitle = subTitle,
            Action = _ => {
                SetVolume(value);
                return true;
            },
            IcoPath = "Images/volume_on.png"
        });
    }

    /// <summary>
    /// Set custom volume
    /// </summary>
    /// <param name="value"></param>
    private void SetVolume(double value) {
        if (_volume.Mute) {
            _volume.Mute = false;
        }

        value = Math.Max(0, Math.Min(100, value));
        _volume.MasterVolumeLevelScalar = (float)value / 100;
        _currentVolume = _volume.MasterVolumeLevelScalar;
    }

    /// <summary>
    /// Mute the speaker
    /// </summary>
    private void Mute() {
        if (_volume.Mute) return;

        _currentVolume = _volume.MasterVolumeLevelScalar;
        _volume.Mute = true;
    }

    /// <summary>
    /// Unmute and restore the previous volume
    /// </summary>
    private void Unmute() {
        _volume.Mute = false;
        
        if (_volume.MasterVolumeLevelScalar == 0) {
            _volume.MasterVolumeLevelScalar = _currentVolume;
        }
    }
}