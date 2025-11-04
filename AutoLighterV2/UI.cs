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
        private TMP_InputField _colorSwitchInputField;
        private TMP_InputField _boostPercentInputField;
        private TMP_InputField _minBoostLenInputField;
        private TMP_InputField _minBrightnessInputField;
        private TMP_InputField _minWallLengthInputField;
        private Toggle _laserColorFadeToggle;
        private Toggle _strobesCenterOnlyToggle;
        private MapEditorUI _mapEditorUI;

        public UI(AutoLighterV2 autolighter)
        {
            _autolighter = autolighter;

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoLighterV2.Icon.png");
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            Texture2D texture2D = new Texture2D(256, 256);
            texture2D.LoadImage(data);

            _extensionBtn.Icon = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height),
                new Vector2(0, 0), 100.0f);
            _extensionBtn.Tooltip = "Autolighter V2";
            ExtensionButtons.AddButton(_extensionBtn);
        }

        public void AddMenu(MapEditorUI mapEditorUI)
        {
            _mapEditorUI = mapEditorUI;
            CanvasGroup parent = mapEditorUI.MainUIGroup[5];
            _autolighterMenu = new GameObject("Autolighter V2 Menu");
            _autolighterMenu.transform.parent = parent.transform;

            AttachTransform(_autolighterMenu, 320, 435, 1, 1, 0, 0, 1, 1);

            Image image = _autolighterMenu.AddComponent<Image>();
            image.sprite = PersistentUI.Instance.Sprites.Background;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.24f, 0.24f, 0.24f);

            AddLabel(_autolighterMenu.transform, "Autolighter V2", "Autolighter V2", new Vector2(0, -18));
            AddResetButton(_autolighterMenu.transform, "ResetDefaults", new Vector2(126, -18),
                () => { ResetToDefaults(); }, "Reset all settings to default values");

            float row = 20f;
            float space = 11f;
            float leftX = -165f;
            float leftXCheck = -104f;
            float rightX = 65f;
            float rightXCheck = 105f;
            float lHeaderX = leftX + 90f;
            float rHeaderX = rightX + 10f;

            float yLeft = -45f;
            AddLabel(_autolighterMenu.transform, "BrightnessHeader", "Brightness", new Vector2(lHeaderX, yLeft));
            yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "UseBrightness", "Dynamic Brightness", new Vector2(leftXCheck, yLeft),
                ConfigManager.Data.UseMapIntensityForBrightness, (check) =>
                {
                    ConfigManager.Data.UseMapIntensityForBrightness = check;
                    ConfigManager.Save();

                    // Enable/disable brightness fields based on dynamic brightness
                    if (_minBrightnessInputField != null) _minBrightnessInputField.interactable = check;
                }, false, tooltip: "Brightness is based on map intensity.\nTakes Max Brightness as fixed value when disabled.");
            yLeft -= row;
            _minBrightnessInputField = AddTextInput(_autolighterMenu.transform, "MinBright", "Min Brightness", new Vector2(leftX, yLeft),
                ConfigManager.Data.MinBrightness.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, Mathf.Min(f, ConfigManager.Data.MaxBrightness));
                        ConfigManager.Data.MinBrightness = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.MinBrightness.ToString("0.00");
                    }
                }, false, tooltip: "Minimum brightness for light events when 'Dynamic Brightness' is enabled.");
            _minBrightnessInputField.interactable = ConfigManager.Data.UseMapIntensityForBrightness;
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "MaxBright", "Max Brightness", new Vector2(leftX, yLeft),
                ConfigManager.Data.MaxBrightness.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(ConfigManager.Data.MinBrightness, f);
                        ConfigManager.Data.MaxBrightness = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.MaxBrightness.ToString("0.00");
                    }
                },
                false, tooltip:
                "Maximum brightness for light events when 'Dynamic Brightness' is enabled.\nOtherwise this is the fixed brightness.");
            yLeft -= row;
            yLeft -= space;
            AddLabel(_autolighterMenu.transform, "ColorsHeader", "Colors", new Vector2(lHeaderX, yLeft));
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "ColorMode", "Color Mode", new Vector2(leftX, yLeft),
                ConfigManager.Data.ColorMode.ToString(), (val, inputField) =>
                {
                    if (int.TryParse(val, out var i))
                    {
                        var clamped = Mathf.Clamp(i, 0, 3);
                        ConfigManager.Data.ColorMode = clamped;
                        ConfigManager.Save();
                        if (clamped != i) inputField.text = clamped.ToString();

                        if (_colorSwitchInputField != null) _colorSwitchInputField.interactable = clamped == 2 || clamped == 3;

                        if (_laserColorFadeToggle != null)
                        {
                            _laserColorFadeToggle.interactable = clamped != 2 && clamped != 3;
                            if (clamped == 2 || clamped == 3)
                            {
                                _laserColorFadeToggle.isOn = false;
                                ConfigManager.Data.LaserColorFade = false;
                                ConfigManager.Save();
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.ColorMode.ToString();
                    }
                }, false, tooltip: "0: Random for each event\n1: Alternating per light\n2: Switch every X beats (Lasers inverted)\n3: Switch every X beats (All lights)");
            yLeft -= row;
            _colorSwitchInputField = AddTextInput(_autolighterMenu.transform, "ColorSwitch", "Color Switch", new Vector2(leftX, yLeft),
                ConfigManager.Data.ColorSwitchBeats.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.ColorSwitchBeats = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.ColorSwitchBeats.ToString("0.00");
                    }
                }, false, tooltip: "Only on Color Mode 2:\nNumber of beats between color switches.");
            _colorSwitchInputField.interactable = ConfigManager.Data.ColorMode == 2;
            yLeft -= row;
            _laserColorFadeToggle = AddCheckbox(_autolighterMenu.transform, "LaserColorFade", "Laser Color Fade", new Vector2(leftXCheck, yLeft),
                ConfigManager.Data.LaserColorFade, (check) =>
                {
                    ConfigManager.Data.LaserColorFade = check;
                    ConfigManager.Save();
                }, false, tooltip: "Colors change during long lasers.\nNo effect on Color Mode 2 and 3.");
            _laserColorFadeToggle.interactable = ConfigManager.Data.ColorMode != 2 && ConfigManager.Data.ColorMode != 3;
            yLeft -= row;
            yLeft -= space;
            AddLabel(_autolighterMenu.transform, "LasersHeader", "Lasers", new Vector2(lHeaderX, yLeft));
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "LaserFade", "Laser Fade", new Vector2(leftX, yLeft),
                ConfigManager.Data.LaserFadeOutLength.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.LaserFadeOutLength = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.LaserFadeOutLength.ToString("0.00");
                    }
                }, false, tooltip: "Length of laser fade out in beats. (Does not influence total length)\nLaser stays full brightness until fade out starts.");
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "LaserSpeedMulti", "Laser Speed Multi", new Vector2(leftX, yLeft),
                ConfigManager.Data.LaserSpeedMulti.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.LaserSpeedMulti = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.LaserSpeedMulti.ToString("0.00");
                    }
                }, false, tooltip: "Laser speed multiplier. Higher = Faster, go brrrrrr.");
            yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "ResetLaserSpeeds", "Reset Laser Speeds", new Vector2(leftXCheck, yLeft),
                ConfigManager.Data.ResetLongLaserSpeeds, (check) =>
                {
                    ConfigManager.Data.ResetLongLaserSpeeds = check;
                    ConfigManager.Save();
                }, false, tooltip: "Laser speeds are set on every laser light event.");
            yLeft -= row;
            yLeft -= space;
            AddLabel(_autolighterMenu.transform, "RingHeader", "Ring", new Vector2(lHeaderX, yLeft));
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "RotInt", "Rotation Interval", new Vector2(leftX, yLeft),
                ConfigManager.Data.RotationInterval.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.RotationInterval = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.RotationInterval.ToString("0.00");
                    }
                }, false, tooltip: "Number of beats between ring rotation events.");
            yLeft -= row;
            AddTextInput(_autolighterMenu.transform, "ZoomInt", "Zoom Interval", new Vector2(leftX, yLeft),
                ConfigManager.Data.ZoomInterval.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.ZoomInterval = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.ZoomInterval.ToString("0.00");
                    }
                }, false, tooltip: "Number of beats between ring zoom events.");
            yLeft -= row;
            AddCheckbox(_autolighterMenu.transform, "DoubleAtIntense", "Dynamic Ring Events", new Vector2(leftXCheck, yLeft),
                ConfigManager.Data.DoubleAtIntenseSections, (check) =>
                {
                    ConfigManager.Data.DoubleAtIntenseSections = check;
                    ConfigManager.Save();
                }, false, tooltip: "The amount of ring rotation and zoom events is doubled during intense sections.");

            float yRight = -45f;
            AddLabel(_autolighterMenu.transform, "GeneralHeader", "General", new Vector2(rHeaderX, yRight));
            yRight -= row;
            AddTextInput(_autolighterMenu.transform, "AntiFlicker", "Anti Flicker", new Vector2(rightX, yRight),
                ConfigManager.Data.AntiFlickerThreshold.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Min(Mathf.Max(0f, f), 4f);
                        ConfigManager.Data.AntiFlickerThreshold = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.AntiFlickerThreshold.ToString("0.00");
                    }
                }, tooltip: "Minimum space between light events in beats.\nDoes not affect wall strobes.");
            yRight -= row;
            AddCheckbox(_autolighterMenu.transform, "RemoveRandomness", "Remove Randomness", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.RemoveRandomness, (check) =>
                {
                    ConfigManager.Data.RemoveRandomness = check;
                    ConfigManager.Save();
                }, tooltip: "Lights always alternate in a fixed pattern instead of randomizing.");
            yRight -= row;
            AddCheckbox(_autolighterMenu.transform, "LightBombs", "Light Bombs", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.LightBombs, (check) =>
                {
                    ConfigManager.Data.LightBombs = check;
                    ConfigManager.Save();
                }, tooltip: "Includes bombs when generating lighting events.\nBombs before first and after last notes are always lit.");
            yRight -= row;
            yRight -= space;
            AddLabel(_autolighterMenu.transform, "WallsHeader", "Walls", new Vector2(rHeaderX, yRight));
            yRight -= row;
            AddCheckbox(_autolighterMenu.transform, "UseWalls", "Use Long Walls", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.UseWalls, (check) =>
                {
                    ConfigManager.Data.UseWalls = check;
                    ConfigManager.Save();

                    if (_minWallLengthInputField != null) _minWallLengthInputField.interactable = check;
                }, tooltip: "Considers long walls for lighting events.");
            yRight -= row;
            _minWallLengthInputField = AddTextInput(_autolighterMenu.transform, "MinWallLen", "Min Wall Length", new Vector2(rightX, yRight),
                ConfigManager.Data.MinWallLength.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.MinWallLength = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.MinWallLength.ToString("0.00");
                    }
                }, tooltip: "Minimum length of walls to be considered for lighting if 'Use Long Walls' is enabled.");
            _minWallLengthInputField.interactable = ConfigManager.Data.UseWalls;
            yRight -= row;
            AddCheckbox(_autolighterMenu.transform, "WallStrobes", "Wall Strobes", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.WallStrobes, (check) =>
                {
                    ConfigManager.Data.WallStrobes = check;
                    ConfigManager.Save();

                    if (_strobesCenterOnlyToggle != null) _strobesCenterOnlyToggle.interactable = check;
                }, tooltip: "Enable strobes on bursts of short walls.");
            yRight -= row;
            _strobesCenterOnlyToggle = AddCheckbox(_autolighterMenu.transform, "CenterStrobes", "Strobes Center Only", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.StrobesCenterOnly, (check) =>
                {
                    ConfigManager.Data.StrobesCenterOnly = check;
                    ConfigManager.Save();
                }, tooltip: "If disabled, strobes use the center and bottom lights.");
            _strobesCenterOnlyToggle.interactable = ConfigManager.Data.WallStrobes;
            yRight -= row;
            AddCheckbox(_autolighterMenu.transform, "WallSprinkles", "Wall Sprinkles", new Vector2(rightXCheck, yRight),
                ConfigManager.Data.WallSprinkles, (check) =>
                {
                    ConfigManager.Data.WallSprinkles = check;
                    ConfigManager.Save();
                }, tooltip: "Enable light flashes on single short walls.");
            yRight -= row;
            yRight -= space;
            AddLabel(_autolighterMenu.transform, "BoostHeader", "Boost", new Vector2(rHeaderX, yRight));
            yRight -= row;
            AddTextInput(_autolighterMenu.transform, "BoostMode", "Boost Mode", new Vector2(rightX, yRight),
                ConfigManager.Data.BoostMode.ToString(), (val, inputField) =>
                {
                    if (int.TryParse(val, out var i))
                    {
                        var clamped = Mathf.Clamp(i, 0, 3);
                        ConfigManager.Data.BoostMode = clamped;
                        ConfigManager.Save();
                        if (clamped != i) inputField.text = clamped.ToString();

                        if (_boostPercentInputField != null) _boostPercentInputField.interactable = clamped == 1;
                        if (_minBoostLenInputField != null) _minBoostLenInputField.interactable = clamped == 1 || clamped == 2;
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.BoostMode.ToString();
                    }
                },
                tooltip:
                "0: No boost\n1: Boost the high intensity parts\n2: Periodic boosts\n3: Keep existing boost events");
            yRight -= row;
            _minBoostLenInputField = AddTextInput(_autolighterMenu.transform, "MinBoostLen", "Boost Length", new Vector2(rightX, yRight),
                ConfigManager.Data.MinBoostLength.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Max(0f, f);
                        ConfigManager.Data.MinBoostLength = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.MinBoostLength.ToString("0.00");
                    }
                },
                tooltip:
                "Boost Mode 1: Minimum boost section length in beats.\nBoost Mode 2: The interval between boosts in beats.");
            _minBoostLenInputField.interactable = ConfigManager.Data.BoostMode == 1 || ConfigManager.Data.BoostMode == 2;
            yRight -= row;
            _boostPercentInputField = AddTextInput(_autolighterMenu.transform, "BoostPercent", "Boost Amount", new Vector2(rightX, yRight),
                ConfigManager.Data.BoostPercent.ToString("0.00"), (val, inputField) =>
                {
                    if (float.TryParse(val, out var f))
                    {
                        var clamped = Mathf.Clamp01(f);
                        ConfigManager.Data.BoostPercent = clamped;
                        ConfigManager.Save();
                        if (Mathf.Abs(clamped - f) > 0.001f) inputField.text = clamped.ToString("0.00");
                    }
                    else if (!string.IsNullOrEmpty(val))
                    {
                        inputField.text = ConfigManager.Data.BoostPercent.ToString("0.00");
                    }
                }, tooltip: "Only on Boost Mode 1:\nHow much of the map should be boosted. (Between 0 and 1)");
            _boostPercentInputField.interactable = ConfigManager.Data.BoostMode == 1;


            var autolightButton = AddButton(_autolighterMenu.transform, "GenLight", "Autolight", new Vector2(-70, -410),
                () => { _autolighter.Light(); }, "Generate lighting events based on the current map.");

            var colorFade = autolightButton.gameObject.AddComponent<ColorFadeAnimation>();
            colorFade.image = autolightButton.Button.GetComponent<Image>();

            AddButton(_autolighterMenu.transform, "SyncDiffs", "Sync to All Diffs", new Vector2(70, -410),
                () => { _autolighter.SyncToAllDiffs(); },
                "Sync the lighting events to all other difficulties in the map.");

            _autolighterMenu.SetActive(false);
            _extensionBtn.Click = () => { _autolighterMenu.SetActive(!_autolighterMenu.activeSelf); };
        }

        private UIButton AddButton(Transform parent, string title, string text, Vector2 pos, UnityAction onClick,
            string tooltip = "")
        {
            var button = Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 120, 28, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText(text);
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 14;

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(button.gameObject, tooltip);
            }

            return button;
        }

        private void AddResetButton(Transform parent, string title, Vector2 pos, UnityAction onClick,
            string tooltip = "")
        {
            var button = Object.Instantiate(PersistentUI.Instance.ButtonPrefab, parent);
            MoveTransform(button.transform, 40, 18, 0.5f, 1, pos.x, pos.y);

            button.name = title;
            button.Button.onClick.AddListener(onClick);

            button.SetText("Reset");
            button.Text.enableAutoSizing = false;
            button.Text.fontSize = 12;
            button.Text.alignment = TextAlignmentOptions.Center;

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

        private TMP_InputField AddTextInput(Transform parent, string title, string text, Vector2 pos, string value,
            UnityAction<string, TMP_InputField> onChange, bool labelLeft = true, string tooltip = "")
        {
            var entryLabel = new GameObject(title + " Label", typeof(TextMeshProUGUI));
            var rectTransform = ((RectTransform)entryLabel.transform);
            rectTransform.SetParent(parent);

            if (labelLeft) MoveTransform(rectTransform, 100, 20, 0.5f, 1, pos.x - 25f, pos.y);
            else MoveTransform(rectTransform, 100, 20, 0.5f, 1, pos.x + 125f, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = labelLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            textComponent.fontSize = 11;
            textComponent.text = text;

            var textInput = Object.Instantiate(PersistentUI.Instance.TextInputPrefab, parent);
            MoveTransform(textInput.transform, 35, 18, 0.5f, 1, pos.x + 50f, pos.y);
            textInput.GetComponent<Image>().pixelsPerUnitMultiplier = 3;
            textInput.InputField.text = value;
            textInput.InputField.onFocusSelectAll = false;
            textInput.InputField.textComponent.alignment = TextAlignmentOptions.Left;
            textInput.InputField.textComponent.fontSize = 10;

            textInput.InputField.onValueChanged.AddListener((val) => onChange(val, textInput.InputField));

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(textInput.gameObject, tooltip);
                AddTooltip(entryLabel, tooltip);
            }

            return textInput.InputField;
        }

        private Toggle AddCheckbox(Transform parent, string title, string text, Vector2 pos, bool value,
            UnityAction<bool> onClick, bool labelLeft = true, string tooltip = "")
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
            if (labelLeft) MoveTransform(rectTransform, 160, 18, 0.5f, 1, pos.x - 95, pos.y);
            else MoveTransform(rectTransform, 160, 18, 0.5f, 1, pos.x + 95, pos.y);
            var textComponent = entryLabel.GetComponent<TextMeshProUGUI>();

            textComponent.name = title;
            textComponent.font = PersistentUI.Instance.ButtonPrefab.Text.font;
            textComponent.alignment = labelLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            textComponent.fontSize = 11;
            textComponent.text = text;

            if (!string.IsNullOrEmpty(tooltip))
            {
                AddTooltip(toggleObject.gameObject, tooltip);
                AddTooltip(entryLabel, tooltip);
            }

            return toggleComponent;
        }

        private void AttachTransform(GameObject obj, float sizeX, float sizeY, float anchorX, float anchorY,
            float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
        {
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
            rectTransform.pivot = new Vector2(pivotX, pivotY);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(anchorX, anchorY);
            rectTransform.anchoredPosition = new Vector3(anchorPosX, anchorPosY, 0);
        }

        private void MoveTransform(Transform transform, float sizeX, float sizeY, float anchorX, float anchorY,
            float anchorPosX, float anchorPosY, float pivotX = 0.5f, float pivotY = 0.5f)
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

        private void ResetToDefaults()
        {
            ConfigManager.Reset();

            Object.Destroy(_autolighterMenu);
            AddMenu(_mapEditorUI);
            _autolighterMenu.SetActive(true);
        }
    }

    public class ColorFadeAnimation : MonoBehaviour
    {
        public Image image;
        private float _time;
        private readonly Color _pink = new Color(0.671f, 0.094f, 0.259f);
        private readonly Color _blue = new Color(0.094f, 0.416f, 0.678f);
        private const float FadeSpeed = 0.25f;

        private void Update()
        {
            if (!image) return;

            _time += Time.deltaTime * FadeSpeed;
            float t = (Mathf.Sin(_time) + 1f) / 2f; // between 0 and 1
            image.color = Color.Lerp(_pink, _blue, t);
        }
    }
}