// SPDX-License-Identifier: MIT
// Original work Copyright (c) 2024 Loloppe
// Modified work Copyright (c) 2025 Jonas00000

using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AutoLighterV2
{
    public class UI
    {
        private GameObject _autolighterMenu;
        private readonly AutoLighterV2 _autolighter;
        private readonly ExtensionButton _extensionBtn = new ExtensionButton();

        public UI(AutoLighterV2 autolighter)
        {
            _autolighter = autolighter;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoLighterV2.Icon.png");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadImage(data);

            _extensionBtn.Icon = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0, 0), 100.0f);
            _extensionBtn.Tooltip = "Autolighter";
            ExtensionButtons.AddButton(_extensionBtn);
        }

        public void AddMenu(MapEditorUI mapEditorUI)
        {
            CanvasGroup parent = mapEditorUI.MainUIGroup[5];
            _autolighterMenu = new GameObject("Autolighter V2 Menu");
            _autolighterMenu.transform.parent = parent.transform;

            AttachTransform(_autolighterMenu, 325, 335, 1, 1, 0, 0, 1, 1);

            Image image = _autolighterMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            AddLabel(_autolighterMenu.transform, "Autolighter V2", "Autolighter V2", new Vector2(0, -18));

            float row = 22f;
            float leftX = -135f;
            float rightX = 60f;

            float yLeft = -50f;
            AddCheckbox(_autolighterMenu.transform, "UseWalls", "Use Long Walls", new Vector2(leftX, yLeft), ConfigManager.Data.UseWalls, (check) => { ConfigManager.Data.UseWalls = check; ConfigManager.Save(); }, "Considers long walls for lighting events."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "WallStrobes", "Wall Strobes", new Vector2(leftX, yLeft), ConfigManager.Data.WallStrobes, (check) => { ConfigManager.Data.WallStrobes = check; ConfigManager.Save(); }, "Enable strobes on bursts of short walls."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "WallSprinkles", "Wall Sprinkles", new Vector2(leftX, yLeft), ConfigManager.Data.WallSprinkles, (check) => { ConfigManager.Data.WallSprinkles = check; ConfigManager.Save(); }, "Enable light flashes on single short walls."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "CenterStrobes", "Strobes Center Only", new Vector2(leftX, yLeft), ConfigManager.Data.StrobesCenterOnly, (check) => { ConfigManager.Data.StrobesCenterOnly = check; ConfigManager.Save(); }, "If disabled, strobes use the center and bottom lights."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "LaserColorFade", "Laser Color Fade", new Vector2(leftX, yLeft), ConfigManager.Data.LaserColorFade, (check) => { ConfigManager.Data.LaserColorFade = check; ConfigManager.Save(); }, tooltip: "Colors change during long lasers."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "ResetLaserSpeeds", "Reset Laser Speeds", new Vector2(leftX, yLeft), ConfigManager.Data.ResetLongLaserSpeeds, (check) => { ConfigManager.Data.ResetLongLaserSpeeds = check; ConfigManager.Save(); }, "Laser speeds are set on every laser light event."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "UseBrightness", "Dynamic Brightness", new Vector2(leftX, yLeft), ConfigManager.Data.UseMapIntensityForBrightness, (check) => { ConfigManager.Data.UseMapIntensityForBrightness = check; ConfigManager.Save(); }, "Brightness is based on map intensity."); yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "DoubleAtIntense", "Dynamic Ring Events", new Vector2(leftX, yLeft), ConfigManager.Data.DoubleAtIntenseSections, (check) => { ConfigManager.Data.DoubleAtIntenseSections = check; ConfigManager.Save(); }, "The amount of ring rotation and zoom events is doubled during intense sections."); yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "BoostMode", "Boost Mode", new Vector2(leftX - 22, yLeft), ConfigManager.Data.BoostMode.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.BoostMode = i; ConfigManager.Save(); } }, false, "0=No boost, 1=Boost the high intensity parts, 2=Periodic boosts, 3=Keep existing boost events"); yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "BoostPercent", "Boost Percentage", new Vector2(leftX - 22, yLeft), ConfigManager.Data.BoostPercent.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.BoostPercent = f; ConfigManager.Save(); } }, false, "Only on Boost Mode 1: How much of the map should be boosted. (Between 0 and 1)"); yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "MinBoostLen", "Min Boost Length", new Vector2(leftX - 22, yLeft), ConfigManager.Data.MinBoostLength.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.MinBoostLength = i; ConfigManager.Save(); } }, false, "Mode 1: Minimum boost section length in beats. Mode 2: The interval between boosts in beats.");

            float yRight = -50f;
            AddTextInput(_autolighterMenu.transform, "LaserSpeedMulti", "Laser Speed Multi", new Vector2(rightX, yRight), ConfigManager.Data.LaserSpeedMulti.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.LaserSpeedMulti = f; ConfigManager.Save(); } }, tooltip: "Laser speed multiplier. Higher = Faster, go brrrrrr."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "MinWallLen", "Min Wall Len", new Vector2(rightX, yRight), ConfigManager.Data.MinWallLength.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.MinWallLength = f; ConfigManager.Save(); } }, tooltip: "Minimum length of walls to be considered for lighting if 'Use Long Walls' is enabled."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "AntiFlicker", "Anti Flicker", new Vector2(rightX, yRight), ConfigManager.Data.AntiFlickerThreshold.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.AntiFlickerThreshold = f; ConfigManager.Save(); } }, tooltip: "Minimum space between light events in beats. Does not affect wall strobes."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "LaserFade", "Laser Fade", new Vector2(rightX, yRight), ConfigManager.Data.LaserFadeOutLength.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.LaserFadeOutLength = f; ConfigManager.Save(); } }, tooltip: "Length of laser color fade out in beats."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "MinBright", "Min Bright", new Vector2(rightX, yRight), ConfigManager.Data.MinBrightness.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.MinBrightness = f; ConfigManager.Save(); } }, tooltip: "Minimum brightness for light events when 'Dynamic Brightness' is enabled."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "MaxBright", "Max Bright", new Vector2(rightX, yRight), ConfigManager.Data.MaxBrightness.ToString("0.00"), (val) => { if (float.TryParse(val, out var f)) { ConfigManager.Data.MaxBrightness = f; ConfigManager.Save(); } }, tooltip: "Maximum brightness for light events when 'Dynamic Brightness' is enabled. Otherwise always max brightness."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "ColorMode", "Color Mode", new Vector2(rightX, yRight), ConfigManager.Data.ColorMode.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.ColorMode = i; ConfigManager.Save(); } }, tooltip: "0=Random for each event, 1=Alternating per light, 2=Global switch every X beats."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "ColorSwitch", "Color Switch", new Vector2(rightX, yRight), ConfigManager.Data.ColorSwitchBeats.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.ColorSwitchBeats = i; ConfigManager.Save(); } }, tooltip: "Only on Color Mode 2: Number of beats between color switches."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "RotInt", "Rot Int", new Vector2(rightX, yRight), ConfigManager.Data.RotationInterval.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.RotationInterval = i; ConfigManager.Save(); } }, tooltip: "Number of beats between ring rotation events."); yRight -= row;
            AddTextInput(_autolighterMenu.transform, "ZoomInt", "Zoom Int", new Vector2(rightX, yRight), ConfigManager.Data.ZoomInterval.ToString(), (val) => { if (int.TryParse(val, out var i)) { ConfigManager.Data.ZoomInterval = i; ConfigManager.Save(); } }, tooltip: "Number of beats between ring zoom events.");

            AddButton(_autolighterMenu.transform, "GenLight", "Autolight", new Vector2(-75, -310), () => { _autolighter.Light(); }, "Generate lighting events based on the current map.");
            AddButton(_autolighterMenu.transform, "SyncDiffs", "Sync to All Diffs", new Vector2(75, -310), () => { _autolighter.SyncToAllDiffs(); }, "Sync the lighting events to all other difficulties in the map.");

            _autolighterMenu.SetActive(false);
            _extensionBtn.Click = () => { _autolighterMenu.SetActive(!_autolighterMenu.activeSelf); };
        }

        private void AddButton(Transform parent, string title, string text, Vector2 pos, UnityAction onClick, string tooltip = "")
        {
            var button = Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 120, 28, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 14;

            if (string.IsNullOrEmpty(tooltip)) return;
            AddTooltip(button.gameObject, tooltip);
        }

        private void AddLabel(Transform parent, string title, string text, Vector2 pos, Vector2? size = null)
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            MoveTransform(rectTransform, 200, 26, 0.5f, 1, pos.x, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 18;
            textComponent.text = text;
        }

        private void AddTextInput(Transform parent, string title, string text, Vector2 pos, string value, UnityAction<string> onChange, bool labelLeft = true, string tooltip = "")
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            if (labelLeft) MoveTransform(rectTransform, 100, 20, 0.5f, 1, pos.x - 45f, pos.y);
            else MoveTransform(rectTransform, 100, 20, 0.5f, 1, pos.x + 150f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = labelLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            textComponent.fontSize = 11;
            textComponent.text = text;

            var textInput = Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            MoveTransform(textInput.transform, 70, 20, 0.5f, 1, pos.x + 50f, pos.y);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = TextAlignmentOptions.Left;
            textInput.InputField.textComponent.fontSize = 11;

            textInput.InputField.onValueChanged.AddListener(onChange);

            if (string.IsNullOrEmpty(tooltip)) return;
            AddTooltip(textInput.gameObject, tooltip);
            AddTooltip(entryLabel, tooltip);
        }

        private void AddCheckbox(Transform parent, string title, string text, Vector2 pos, bool value, UnityAction<bool> onClick, string tooltip = "")
        {
            var original = GameObject.Find("Strobe Generator").GetComponentInChildren<Toggle>(true);
            var toggleObject = Object.Instantiate(original, parent.transform);
            MoveTransform(toggleObject.transform, 16, 16, 0.5f, 1, pos.x, pos.y);

            var toggleComponent = toggleObject.GetComponent<Toggle>();
            var colorBlock = toggleComponent.colors;
            colorBlock.normalColor = Color.white;
            toggleComponent.colors = colorBlock;
            toggleComponent.isOn = value;
            toggleComponent.onValueChanged.AddListener(onClick);

            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);
            MoveTransform(rectTransform, 160, 18, 0.5f, 1, pos.x + 90, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.fontSize = 11;
            textComponent.text = text;

            if (string.IsNullOrEmpty(tooltip)) return;
            AddTooltip(toggleObject.gameObject, tooltip);
            AddTooltip(entryLabel, tooltip);
        }

        private void AttachTransform(GameObject obj, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        private void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY, float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            if (!(transform is RectTransform rectTransform)) return;

            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        private void AddTooltip(GameObject obj, string tooltipText)
        {
            var tooltip = obj.AddComponent<Tooltip>();
            tooltip.TooltipOverride = tooltipText;
        }
    }
}