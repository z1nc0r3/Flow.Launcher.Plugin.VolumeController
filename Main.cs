using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using Flow.Launcher.Plugin;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.Net;
using System.ComponentModel;

namespace Flow.Launcher.Plugin.VolumeController {
    public class VolumeController : IPlugin {

        private PluginInitContext context;

        private readonly List<Result> _results = new List<Result>();
        private MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();
        private MMDevice _defaultPlaybackDevice;
        private AudioEndpointVolume _volume;

        private float currentVolume;
        private bool isMute;

        public void Init(PluginInitContext context) {
            this.context = context;
            _defaultPlaybackDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _volume = _defaultPlaybackDevice.AudioEndpointVolume;

            if (_defaultPlaybackDevice == null) {
                throw new Exception("No audio endpoint found.");
            }
            _volume = _defaultPlaybackDevice.AudioEndpointVolume;
            if (_volume == null) {
                throw new Exception("Failed to get audio volume control.");
            }
        }

        public List<Result> Query(Query query) {
            _results.Clear();

            // Mute or unmute
            if (query.Search == "mute" || query.Search == "unmute") {
                bool mute = query.Search == "mute";
                _volume.Mute = mute;
                _results.Add(new Result() {
                    Title = $"Turn {query.Search} sound",
                    SubTitle = "Click to confirm",
                    Action = _ => {
                        if (mute == _volume.Mute) {
                            Mute();
                        }
                        return true;
                    },
                });
            } else // Set volume level
              {
                if (double.TryParse(query.Search, out double value)) {
                    value = Math.Max(0, Math.Min(100, value));
                    _volume.MasterVolumeLevelScalar = (float)(value / 100);
                    _results.Add(new Result() {
                        Title = $"Set volume to {value}",
                        SubTitle = "Click to confirm",
                        Action = _ => {
                            SetVolume(value);
                            return true;
                        },
                    });
                }
            }

            return _results;
        }

        public void SetVolume(double value) {
            value = Math.Max(0, Math.Min(100, value)); // ensure value is between 0 and 100
            _volume.MasterVolumeLevelScalar = (float)value / 100;
            currentVolume = _volume.MasterVolumeLevelScalar;
        }

        public void Mute() {
            if (isMute) return;

            currentVolume = _volume.MasterVolumeLevelScalar;
            _volume.MasterVolumeLevelScalar = 0;

            isMute = true;
        }

        public void Unmute() {
            _volume.MasterVolumeLevelScalar = currentVolume;
            isMute = false;
        }
    }
}