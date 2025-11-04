// SPDX-License-Identifier: MIT
// Original work Copyright (c) 2024 Loloppe
// Modified work Copyright (c) 2025 Jonas00000

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using UnityEngine;
using Beatmap.Base;
using Beatmap.Base.Customs;

namespace AutoLighterV2
{
    [Plugin("AutolighterV2")]
    public class AutoLighterV2
    {
        private UI _ui;
        private NoteGridContainer _noteGridContainer;
        private EventGridContainer _eventGridContainer;

        private static string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "2.1.0.0";
        }

        [Init]
        private void Init()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            ConfigManager.Load();
            _ui = new UI(this);
        }

        private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.buildIndex != 3) return;
            _noteGridContainer = Object.FindObjectOfType<NoteGridContainer>();
            _eventGridContainer = Object.FindObjectOfType<EventGridContainer>();

            var mapEditorUI = Object.FindObjectOfType<MapEditorUI>();
            if (mapEditorUI != null)
            {
                _ui.AddMenu(mapEditorUI);
            }
        }

        public void Light()
        {
            if (_noteGridContainer?.MapObjects == null || !_noteGridContainer.MapObjects.Any())
            {
                PersistentUI.Instance.ShowDialogBox("No notes found! Please add notes to your map before autolighting.", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            var arcContainer = Object.FindObjectOfType<ArcGridContainer>();
            var arcs = arcContainer?.MapObjects?.ToList() ?? new List<BaseArc>();
            var sliders = new List<BaseSlider>(arcs.Cast<BaseSlider>());

            var state = new MapEditorState
            {
                Notes = _noteGridContainer.MapObjects.ToList(),
                Obstacles = Object.FindObjectOfType<ObstacleGridContainer>()?.MapObjects?.ToList() ?? new List<BaseObstacle>(),
                Sliders = sliders,
                ExistingBoosts = _eventGridContainer.MapObjects.Where(e => e.Type == 5).ToList(),
                Bookmarks = (BeatSaberSongContainer.Instance?.Map?.Bookmarks) ?? (new List<BaseBookmark>()),
            };

            var toDelete = _eventGridContainer.MapObjects.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();
            foreach (var ev in toDelete) _eventGridContainer.DeleteObject(ev, false);

            var cfg = ConfigManager.Data;
            var newEvents = EventGenerator.GenerateAll(state, cfg);

            foreach (var ev in newEvents) _eventGridContainer.SpawnObject(ev, false, false, true);
            _eventGridContainer.DoPostObjectsSpawnedWorkflow();
            _eventGridContainer.RefreshPool(true);

            if (BeatSaberSongContainer.Instance?.Map != null)
            {
                var map = BeatSaberSongContainer.Instance.Map;
                var version = GetVersion();
                string lighterFieldName = (map.MajorVersion == 2) ? "_lighter" : "lighter";

                SimpleJSON.JSONNode lighterData;
                if (map.CustomData.HasKey(lighterFieldName) && map.CustomData[lighterFieldName].IsObject)
                {
                    lighterData = map.CustomData[lighterFieldName];
                }
                else
                {
                    lighterData = new SimpleJSON.JSONObject();
                    map.CustomData[lighterFieldName] = lighterData;
                }

                var autoLighterData = new SimpleJSON.JSONObject();
                autoLighterData["version"] = version;
                lighterData["AutoLighterV2 by Jonas"] = autoLighterData;

                if (map.MajorVersion == 4 && BeatSaberSongContainer.Instance?.MapDifficultyInfo != null)
                {
                    var diffInfo = BeatSaberSongContainer.Instance.MapDifficultyInfo;
                    if (!diffInfo.Lighters.Contains("AutoLighterV2 by Jonas"))
                    {
                        diffInfo.Lighters.Add("AutoLighterV2 by Jonas");
                    }
                }
            }

            ConfigManager.Save();
        }

        public void SyncToAllDiffs()
        {
            if (BeatSaberSongContainer.Instance?.Info == null || BeatSaberSongContainer.Instance?.Map == null)
            {
                PersistentUI.Instance.ShowDialogBox("No map loaded!", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            var currentMap = BeatSaberSongContainer.Instance.Map;
            var info = BeatSaberSongContainer.Instance.Info;
            var currentDiffInfo = BeatSaberSongContainer.Instance.MapDifficultyInfo;

            var lightEvents = currentMap.Events.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();

            if (lightEvents.Count == 0)
            {
                PersistentUI.Instance.ShowDialogBox("No lights to sync! Generate lights first.", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }

            string lighterFieldName = currentMap.MajorVersion == 2 ? "_lighter" : "lighter";
            SimpleJSON.JSONNode lighterMetadata = null;
            if (currentMap.CustomData.HasKey(lighterFieldName) && currentMap.CustomData[lighterFieldName].IsObject)
                lighterMetadata = currentMap.CustomData[lighterFieldName].Clone();

            int syncedCount = 0;
            foreach (var diffSet in info.DifficultySets)
            {
                foreach (var diff in diffSet.Difficulties)
                {
                    if (diff == currentDiffInfo) continue;

                    try
                    {
                        var targetMap = BeatSaberSongUtils.GetMapFromInfoFiles(info, diff);
                        if (targetMap == null) continue;

                        var toRemove = targetMap.Events.Where(ev => !new List<int>() { 14, 15, 100 }.Contains(ev.Type)).ToList();
                        foreach (var ev in toRemove)
                        {
                            targetMap.Events.Remove(ev);
                        }

                        foreach (var lightEvent in lightEvents)
                        {
                            targetMap.Events.Add(lightEvent.Clone() as BaseEvent);
                        }

                        if (lighterMetadata != null)
                        {
                            string targetLighterFieldName = targetMap.MajorVersion == 2 ? "_lighter" : "lighter";
                            targetMap.CustomData[targetLighterFieldName] = lighterMetadata.Clone();
                        }

                        if (info.MajorVersion == 4 && currentDiffInfo.Lighters.Count > 0)
                        {
                            diff.Lighters.Clear();
                            foreach (var lighter in currentDiffInfo.Lighters)
                            {
                                diff.Lighters.Add(lighter);
                            }
                        }

                        targetMap.Save();
                        syncedCount++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to sync lights to {diff.Difficulty} ({diffSet.Characteristic}): {ex.Message}");
                    }
                }
            }

            if (info.MajorVersion == 4) info.Save();

            if (syncedCount == 0)
            {
                PersistentUI.Instance.ShowDialogBox("No other difficulties found to sync!", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
            PersistentUI.Instance.ShowDialogBox($"Synced lights to {syncedCount} difficulties!", null, PersistentUI.DialogBoxPresetType.Ok);
        }

        [Exit]
        private void Exit()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
        }
    }
}