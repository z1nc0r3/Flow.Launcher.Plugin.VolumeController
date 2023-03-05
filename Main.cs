using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using Flow.Launcher.Plugin;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using System.Net;
using System.ComponentModel;
using System.Collections;
using System.Linq;
using FuzzySharp;
using System.Text.RegularExpressions;
using NAudio.Utils;

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
            var commands = Commands(query);
            return commands;
        }

        private List<Result> Commands(Query query) {
            var results = new List<Result>();

            // Get the search query from user input
            string search = query.Search.Trim().ToLower();

            // Check if user typed "mute" or a similar keyword
            if (search.StartsWith("m")) {
                results.Add(new Result {
                    Title = "Mute",
                    SubTitle = "Mute the volume",
                    Action = c => {
                        Mute();
                        return true;
                    }
                });
            } else if (search.StartsWith("u")) {
                results.Add(new Result {
                    Title = "Unmute",
                    SubTitle = "Restore the volume",
                    Action = c => {
                        Unmute();
                        return true;
                    }
                });
            } else if (double.TryParse(query.Search, out double value)) {
                results.Add(new Result {
                    Title = "Set Volume",
                    SubTitle = $"Set volume to {value}",
                    Action = c => {
                        SetVolume(value);
                        return true;
                    },
                    IcoPath = "Images/app.png"
                });
            } else {
                results.AddRange(new[]
                {
                    new Result
                    {
                        Title = "Mute",
                        SubTitle = "Mute the volume",
                        Action = c =>
                        {
                            Mute();
                            return true;
                        }
                    },
                    new Result
                    {
                        Title = "Unmute",
                        SubTitle = "Restore the volume",
                        Action = c =>
                        {
                            Unmute();
                            return true;
                        }
                    },
                    new Result
                    {
                        Title = "Set Volume",
                        SubTitle = $"Query: {(query.Search)}",
                        Action = c =>
                        {
                            SetVolume(double.Parse(query.Search));
                            return true;
                        }
                    }
                });

                if (query.Search.Length > 0 && !double.TryParse(query.Search, out double val)) {
                    foreach (var result in results) {
                        if (result.Title.Equals("Set Volume")) {
                            results.Remove(result);
                            break;
                        }
                    }
                }
            }
            
            return results;
        }

        public void SetVolume(double value) {
            value = Math.Max(0, Math.Min(100, value)); // ensure value is between 0 and 100
            _volume.MasterVolumeLevelScalar = (float)value / 100;
            currentVolume = _volume.MasterVolumeLevelScalar;

            /*if (Regex.IsMatch(value, @"^\d")) {
                results.Add(new Result {
                    Title = "Invalid input",
                    SubTitle = "Type 'vol' to see available commands",
                    IcoPath = "Images/app.png"
                });
            }*/
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