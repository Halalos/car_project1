using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cars
{
    public sealed class CarsUI : MonoBehaviour
    {
        public GameSession session;
        public Text statusLabel;
        public Text hudLabel;
        public Text raceLabel;
        public Text resultLabel;
        public Text selectionLabel;
        public Text garageLabel;
        public Text notificationLabel;
        public Image damageOverlay;
        public string ResultReason { get; set; }
        float notifyUntil;
        bool restoreArmed;
        Font font;
        static readonly Color Ink = new Color(.025f, .012f, .085f, .94f);
        static readonly Color Accent = new Color(.12f, .94f, 1f);
        public void BuildLayout()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var loading = Screen("Loading"); Label(loading, "Loading local profile...", 0, 0, 1200, 100, 32);
            var main = Screen("Main");
            Label(main, "VELOCITY // AFTER DARK", 0, 280, 1200, 110, 64, Accent);
            Label(main, "NEON NIGHTS. THREE WAYS TO RACE.", 0, 195, 1000, 50, 23);
            Button(main, "Select", "PLAY", 0, 75, 410, 72);
            Button(main, "Garage", "GARAGE & UPGRADES", 0, -20, 410, 65);
            Button(main, "Sync", "SYNC CLOUD", -110, -110, 200, 55);
            Button(main, "Restore", "RESTORE CLOUD", 110, -110, 200, 55);
            Button(main, "Quit", "QUIT", 0, -200, 410, 55);
            Button(main, "Sound", "SOUND: ON", 500, -200, 260, 55);
            Label(main, "A / D or arrows: steer     SPACE: boost     ESC: pause\nAutomatic acceleration. Avoid traffic; spend rewards in the garage.", 0, -310, 1400, 85, 22);
            var select = Screen("Select");
            Label(select, "CHOOSE YOUR DRIVE", 0, 320, 1300, 90, 44, Accent);
            Label(select, "GAME MODE", -470, 225, 400, 55, 25);
            Label(select, "ENVIRONMENT", 0, 225, 400, 55, 25);
            Label(select, "LEVEL", 470, 225, 400, 55, 25);
            Button(select, "Endless", "ENDLESS SURVIVAL", -470, 140, 400, 65);
            Button(select, "Race", "RACE / 5 RIVALS", -470, 45, 400, 65);
            Button(select, "Time", "TIME ATTACK", -470, -50, 400, 65);
            Button(select, "Valley", "NEON VALLEY", 0, 140, 400, 65);
            Button(select, "Desert", "MIDNIGHT DESERT", 0, 45, 400, 65);
            Button(select, "Level0", "ROOKIE", 470, 140, 400, 65);
            Button(select, "Level1", "CHALLENGER", 470, 45, 400, 65);
            Button(select, "Level2", "MASTER", 470, -50, 400, 65);
            selectionLabel = Label(select, "", 0, -180, 1420, 120, 25);
            Button(select, "Back", "BACK", -235, -310, 400, 65);
            Button(select, "Start", "START ENGINE", 235, -310, 400, 65);
            var garage = Screen("Garage");
            Label(garage, "GARAGE / PERFORMANCE LAB", 0, 320, 1400, 90, 44, Accent);
            garageLabel = Label(garage, "", 0, 95, 1350, 300, 29);
            Button(garage, "Speed", "UPGRADE SPEED", -450, -140, 400, 65);
            Button(garage, "Acceleration", "UPGRADE ACCELERATION", 0, -140, 400, 65);
            Button(garage, "Handling", "UPGRADE HANDLING", 450, -140, 400, 65);
            Button(garage, "Back", "BACK", 0, -300, 400, 65);
            var hud = Screen("HUD", false);
            hudLabel = Label(hud, "", -500, 315, 560, 210, 29); hudLabel.alignment = TextAnchor.UpperLeft;
            raceLabel = Label(hud, "", 500, 315, 560, 210, 29); raceLabel.alignment = TextAnchor.UpperRight;
            Button(hud, "PauseButton", "II  PAUSE", 625, -330, 260, 60);
            Label(hud, "A / D   STEER         SPACE   BOOST", 0, -345, 650, 50, 20);
            var pause = Screen("Pause");
            Label(pause, "PAUSED", 0, 220, 1000, 100, 58, Accent);
            Button(pause, "Resume", "RESUME", 0, 60, 450, 70);
            Button(pause, "Retry", "RESTART", 0, -40, 450, 70);
            Button(pause, "Menu", "MAIN MENU", 0, -140, 450, 70);
            Button(pause, "Sound", "SOUND: ON", -125, -240, 220, 55);
            Button(pause, "Quit", "QUIT", 125, -240, 220, 55);
            var result = Screen("Result");
            resultLabel = Label(result, "", 0, 95, 1400, 440, 34);
            Button(result, "Retry", "RACE AGAIN", -235, -240, 400, 65);
            Button(result, "Menu", "MAIN MENU", 235, -240, 400, 65);
            var overlay = new GameObject("Damage", typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(transform, false);
            damageOverlay = overlay.GetComponent<Image>(); damageOverlay.color = Color.clear; damageOverlay.raycastTarget = false;
            Stretch(overlay.GetComponent<RectTransform>());
            notificationLabel = Label(transform, "", 0, 250, 1400, 80, 35, Accent); notificationLabel.raycastTarget = false;
            statusLabel = Label(transform, "Loading...", 0, -415, 1500, 45, 19); statusLabel.raycastTarget = false;
            Show("Loading");
        }
        void Start()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                string id = button.gameObject.name;
                button.onClick.AddListener(() => Click(id));
            }
        }
        void Click(string id)
        {
            if (session == null) return;
            if (session.audioEffects != null) session.audioEffects.Play(SoundCue.Click);
            if (id == "Quit") { session.Quit(); return; }
            if (id == "Sound") { if (session.audioEffects != null) session.audioEffects.ToggleSound(); RefreshSoundLabels(); return; }
            if (session.Saves == null || !session.Saves.Ready || session.IsQuitting) return;
            switch (id)
            {
                case "Select": Show("Select"); break;
                case "Garage": Show("Garage"); break;
                case "Back": Show("Main"); break;
                case "Endless": session.mode = GameMode.EndlessSurvival; break;
                case "Race": session.mode = GameMode.Race; break;
                case "Time": session.mode = GameMode.TimeAttack; break;
                case "Valley": session.environment = EnvironmentKind.GreenValley; break;
                case "Desert": session.environment = EnvironmentKind.SunsetDesert; break;
                case "Level0": session.level = 0; break;
                case "Level1": session.level = 1; break;
                case "Level2": session.level = 2; break;
                case "Start": session.Launch(); break;
                case "PauseButton": session.Pause(); break;
                case "Resume": session.Resume(); break;
                case "Retry": session.Begin(); break;
                case "Menu": session.MainMenu(); break;
                case "Quit": session.Quit(); break;
                case "Speed": session.Purchase(UpgradeKind.Speed); break;
                case "Acceleration": session.Purchase(UpgradeKind.Acceleration); break;
                case "Handling": session.Purchase(UpgradeKind.Handling); break;
                case "Sync": session.CloudSync(false); break;
                case "Restore":
                    if (restoreArmed) { restoreArmed = false; session.CloudSync(true); }
                    else { restoreArmed = true; Notify("Restore replaces the local profile. Click RESTORE again to confirm."); }
                    break;
            }
            if (id != "Restore") restoreArmed = false;
        }
        void RefreshSoundLabels()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == "Sound") button.GetComponentInChildren<Text>().text =
                    session != null && session.audioEffects != null && session.audioEffects.MasterVolume > 0 ? "SOUND: ON" : "SOUND: OFF";
        }
        public void Show(string name)
        {
            RefreshSoundLabels();
            foreach (Transform child in transform)
                if (child.name.StartsWith("Screen_")) child.gameObject.SetActive(child.name == "Screen_" + name);
        }
        public void Notify(string text) { notificationLabel.text = text; notifyUntil = Time.unscaledTime + 4; }
        void Update()
        {
            if (session == null || session.Saves == null) return;
            statusLabel.text = session.Saves.Status + (session.Saves.CloudBusy ? "  [Please wait]" : "");
            if (Time.unscaledTime > notifyUntil) notificationLabel.text = "";
            if (!session.Saves.Ready) return;
            var profile = session.Saves.Profile;
            var properties = session.config.vehicle.WithUpgrades(profile);
            string modeName = session.mode == GameMode.Race ? "Race" : session.mode == GameMode.TimeAttack ? "Time Attack" : "Endless Survival";
            string key = session.mode + "_" + session.environment + "_" + session.level;
            var best = profile.best.Find(x => x.key == key);
            selectionLabel.text = modeName + "  /  " + (session.environment == EnvironmentKind.GreenValley ? "Neon Valley" : "Midnight Desert") + "  /  " + session.config.levels[session.level].title +
                "\n" + (best == null ? "No record yet" : "BEST SCORE " + best.score + (best.completed ? "  |  BEST FINISH " + best.time.ToString("F1") + "s" : "")) +
                "\n" + (session.mode == GameMode.EndlessSurvival ? "Survive collisions and collect points." : session.mode == GameMode.Race ? "Finish first to win. SPACE boost recharges over time." : "Reach the finish before time expires. Checkpoints add 5 seconds.");
            garageLabel.text = "WALLET  " + profile.coins + " COINS\n\n" + UpgradeLine("TOP SPEED", UpgradeKind.Speed, RaceRules.Kmh(properties.topSpeed).ToString("F0") + " km/h") +
                "\n" + UpgradeLine("ACCELERATION", UpgradeKind.Acceleration, properties.acceleration.ToString("F1") + " m/s²") +
                "\n" + UpgradeLine("HANDLING", UpgradeKind.Handling, properties.handling.ToString("F1") + " m/s lateral");
            if (session.Player != null)
            {
                var player = session.Player;
                hudLabel.text = RaceRules.Kmh(player.Speed).ToString("F0") + " KM/H\nSCORE  " + session.Score +
                    "\nDISTANCE  " + player.Distance.ToString("F0") + " m\nTIME  " + session.Elapsed.ToString("F1") + " s";
                raceLabel.text = modeName.ToUpperInvariant() + "\n" + (session.mode == GameMode.Race ? "POSITION  " + session.Position + " / 6" : "POSITION  —") +
                    "\n" + (session.mode == GameMode.TimeAttack ? "REMAINING  " + Mathf.Max(0, session.Remaining).ToString("F1") + " s" : "CONDITION  " + player.Integrity.ToString("F0") + "%") +
                    "\nBOOST  " + (player.Boost * 100).ToString("F0") + "%";
                damageOverlay.color = new Color(1, .08f, .02f, player.HitFlash * .22f);
            }
            if (session.Result != null)
            {
                var result = session.Result;
                resultLabel.text = (result.won ? "VICTORY" : session.mode == GameMode.EndlessSurvival ? "RUN COMPLETE" : "DEFEAT") +
                    "\n\n" + ResultReason + "\nSCORE  " + result.score + "     DISTANCE  " + result.distance.ToString("F0") + " m" +
                    "\nTIME  " + result.elapsed.ToString("F1") + " s" + (result.mode == GameMode.Race ? "     POSITION  " + result.position + " / 6" : "") +
                    "\n\n+ " + result.earnedCoins + " COINS     WALLET  " + profile.coins;
            }
        }
        string UpgradeLine(string title, UpgradeKind kind, string value)
        {
            int level = session.Saves.Profile.GetLevel(kind);
            return title + "   " + value + "   [" + level + "/5]   " +
                (level >= session.config.maxUpgradeLevel ? "MAX" : session.config.UpgradeCost(level) + " coins");
        }
        Transform Screen(string title, bool background = true)
        {
            var go = new GameObject("Screen_" + title, typeof(RectTransform)); go.transform.SetParent(transform, false);
            Stretch(go.GetComponent<RectTransform>());
            if (background) { var image = go.AddComponent<Image>(); image.color = Ink; }
            return go.transform;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
            var label = go.GetComponent<Text>(); label.font = font; label.text = text; label.fontSize = size;
            label.color = color ?? Color.white; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        void Button(Transform parent, string id, string title, float x, float y, float w, float h)
        {
            var go = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
            go.GetComponent<Image>().color = new Color(.11f, .045f, .23f);
            var outline = go.AddComponent<Outline>(); outline.effectColor = new Color(.08f,.78f,1,.8f); outline.effectDistance = new Vector2(1.5f,-1.5f);
            var button = go.GetComponent<Button>(); var colors = button.colors;
            colors.highlightedColor = new Color(.3f, 1, .8f); colors.pressedColor = new Color(.2f, .6f, .5f); button.colors = colors;
            var label = Label(go.transform, title, 0, 0, w - 10, h - 4, 23);
            label.fontStyle = FontStyle.Bold;
        }
    }
}
