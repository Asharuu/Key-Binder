using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SafeKeyBinder.Models;
using SafeKeyBinder.Native;
using SafeKeyBinder.Services;
using SafeKeyBinder.UI;

namespace SafeKeyBinder.Tests
{
    internal static class SafeKeyBinderTests
    {
        private static int _passed = 0;
        private static int _failed = 0;

        [STAThread]
        private static int Main(string[] args)
        {
            Console.WriteLine("=== SafeKeyBinder Automated Verification ===");

            RunTest("Win32 INPUT struct size matches platform ABI", TestInputStructSize);
            RunTest("ConfigService roundtrip saves and loads Unicode keybinds", TestConfigRoundtrip);
            RunTest("KeybindMatcher Global mode matches exact modifiers only", TestMatcherGlobalAndModifiers);
            RunTest("KeybindMatcher WindowFilter matches process and title", TestMatcherWindowFilter);
            RunTest("KeybindMatcher MasterToggle toggles and suppresses", TestMatcherMasterToggle);
            RunTest("KeybindMatcher Optional TypingPause pauses and Enter/Esc resumes", TestMatcherTypingPause);
            RunTest("KeybindMatcher Disabled item or Stopped state does not fire", TestMatcherDisabledAndStopped);
            RunTest("Security Audit: Zero networking namespaces in source files", TestNoNetworkApisInSource);
            RunTest("Integration: KeyboardHookService installs & uninstalls WH_KEYBOARD_LL", TestHookInstallAndUninstall);
            RunTest("Integration: MainForm initializes cleanly without runtime errors", TestMainFormInitialization);
            RunTest("SequenceGenerator generates letter ranges (77a..77z and aa..az)", TestSequenceGeneratorLetters);
            RunTest("SequenceGenerator generates padded numbers & validates invalid ranges", TestSequenceGeneratorNumbersAndValidation);
            RunTest("SequenceGenerator formats {item} command template properly", TestSequenceFormatCommand);
            RunTest("ConfigService preserves AutoSequenceConfig across XML roundtrip", TestAutoSequenceConfigRoundtrip);

            Console.WriteLine();
            Console.WriteLine(string.Format("Results: {0} passed, {1} failed", _passed, _failed));
            return _failed == 0 ? 0 : 1;
        }

        private static void RunTest(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine("[FAIL] " + name + " -> " + ex.Message);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new Exception(string.Format("{0} (expected: {1}, actual: {2})", message, expected, actual));
            }
        }

        private static void TestInputStructSize()
        {
            int actualSize = Marshal.SizeOf(typeof(Win32Native.INPUT));
            int expectedSize = IntPtr.Size == 8 ? 40 : 28;
            AssertEqual(expectedSize, actualSize, "INPUT struct size mismatch for SendInput");
        }

        private static void TestConfigRoundtrip()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "SafeKeyBinder_Test_" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                ConfigService svc = new ConfigService(tempFile);
                AppConfig cfg = AppConfig.CreateDefault();
                cfg.TargetScope = TargetScopeMode.WindowFilter;
                cfg.WindowFilterKeyword = "[notepad] Catatan";
                cfg.TypingPauseEnabled = true;
                cfg.TypingDelayMs = 5;

                KeybindItem custom = new KeybindItem();
                custom.Label = "Unicode Test";
                custom.VirtualKey = 0x31; // '1'
                custom.RequireCtrl = true;
                custom.TextToSend = "Halo Dunia!\nBaris 2 — ✓";
                custom.PrefixVirtualKey = 0x54;
                custom.Suffix = SuffixAction.Enter;
                cfg.Keybinds.Add(custom);

                svc.Save(cfg);
                AppConfig loaded = svc.Load();

                AssertEqual(TargetScopeMode.WindowFilter, loaded.TargetScope, "TargetScope mismatch");
                AssertEqual("[notepad] Catatan", loaded.WindowFilterKeyword, "WindowFilterKeyword mismatch");
                AssertEqual(true, loaded.TypingPauseEnabled, "TypingPauseEnabled mismatch");
                AssertEqual(5, loaded.TypingDelayMs, "TypingDelayMs mismatch");
                AssertEqual(3, loaded.Keybinds.Count, "Keybind count mismatch");
                AssertEqual("Halo Dunia!\nBaris 2 — ✓", loaded.Keybinds[2].TextToSend, "Unicode multiline text mismatch");
                AssertEqual(SuffixAction.Enter, loaded.Keybinds[2].Suffix, "Suffix mismatch");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        private static void TestMatcherGlobalAndModifiers()
        {
            AppConfig cfg = new AppConfig();
            KeybindItem plain1 = new KeybindItem { Label = "Plain1", VirtualKey = 0x31, TextToSend = "satu" };
            KeybindItem ctrl1 = new KeybindItem { Label = "Ctrl1", VirtualKey = 0x31, RequireCtrl = true, TextToSend = "ctrl-satu" };
            cfg.Keybinds.Add(plain1);
            cfg.Keybinds.Add(ctrl1);

            HookMatchResult r1 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x31, false, false, false, null);
            AssertEqual(HookMatchAction.TriggerKeybind, r1.Action, "Should match plain 1");
            AssertEqual("Plain1", r1.MatchedItem.Label, "Matched wrong item for plain 1");
            AssertTrue(r1.SuppressKeystroke, "Should suppress matched key");

            HookMatchResult r2 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x31, true, false, false, null);
            AssertEqual(HookMatchAction.TriggerKeybind, r2.Action, "Should match Ctrl+1");
            AssertEqual("Ctrl1", r2.MatchedItem.Label, "Matched wrong item for Ctrl+1");

            HookMatchResult r3 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x31, true, true, false, null);
            AssertEqual(HookMatchAction.None, r3.Action, "Should not match Ctrl+Alt+1");
        }

        private static void TestMatcherWindowFilter()
        {
            AppConfig cfg = new AppConfig();
            cfg.TargetScope = TargetScopeMode.WindowFilter;
            cfg.WindowFilterKeyword = "[notepad] Untitled";
            cfg.Keybinds.Add(new KeybindItem { Label = "F8", VirtualKey = 0x77, TextToSend = "hi" });

            WindowInfo notepadWin = new WindowInfo { ProcessName = "notepad", Title = "Untitled - Notepad" };
            WindowInfo chromeWin = new WindowInfo { ProcessName = "chrome", Title = "Google Chrome" };

            HookMatchResult m1 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x77, false, false, false, notepadWin);
            AssertEqual(HookMatchAction.TriggerKeybind, m1.Action, "Should match target window");

            HookMatchResult m2 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x77, false, false, false, chromeWin);
            AssertEqual(HookMatchAction.None, m2.Action, "Should ignore non-target window");
        }

        private static void TestMatcherMasterToggle()
        {
            AppConfig cfg = new AppConfig();
            cfg.MasterToggleVirtualKey = 0x13; // Pause

            HookMatchResult m1 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x13, false, false, false, null);
            AssertEqual(HookMatchAction.ToggleMaster, m1.Action, "Master toggle should fire when Active");
            AssertTrue(m1.SuppressKeystroke, "Master toggle should suppress Pause key");

            HookMatchResult m2 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.PausedByMaster, 0x13, false, false, false, null);
            AssertEqual(HookMatchAction.ToggleMaster, m2.Action, "Master toggle should fire when PausedByMaster");
        }

        private static void TestMatcherTypingPause()
        {
            AppConfig cfg = new AppConfig();
            cfg.TypingPauseEnabled = true;
            cfg.TypingPauseVirtualKey = 0x54; // 'T'
            cfg.Keybinds.Add(new KeybindItem { Label = "F8", VirtualKey = 0x77, TextToSend = "hi" });

            HookMatchResult p1 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x54, false, false, false, null);
            AssertEqual(HookMatchAction.TypingPause, p1.Action, "T should trigger TypingPause");
            AssertTrue(!p1.SuppressKeystroke, "T must pass through so user can type it");

            HookMatchResult blocked = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.PausedByTyping, 0x77, false, false, false, null);
            AssertEqual(HookMatchAction.None, blocked.Action, "F8 must not fire while PausedByTyping");

            HookMatchResult resEnter = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.PausedByTyping, Win32Native.VK_RETURN, false, false, false, null);
            AssertEqual(HookMatchAction.TypingResume, resEnter.Action, "Enter should resume from PausedByTyping");
            AssertTrue(!resEnter.SuppressKeystroke, "Enter must pass through");

            HookMatchResult resEsc = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.PausedByTyping, Win32Native.VK_ESCAPE, false, false, false, null);
            AssertEqual(HookMatchAction.TypingResume, resEsc.Action, "Esc should resume from PausedByTyping");
        }

        private static void TestMatcherDisabledAndStopped()
        {
            AppConfig cfg = new AppConfig();
            cfg.Keybinds.Add(new KeybindItem { Enabled = false, VirtualKey = 0x77, TextToSend = "off" });

            HookMatchResult r1 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Active, 0x77, false, false, false, null);
            AssertEqual(HookMatchAction.None, r1.Action, "Disabled keybind must not match");

            cfg.Keybinds[0].Enabled = true;
            HookMatchResult r2 = KeybindMatcher.EvaluateKeyDown(cfg, BinderStatus.Stopped, 0x77, false, false, false, null);
            AssertEqual(HookMatchAction.None, r2.Action, "Stopped state must not match anything");
        }

        private static void TestNoNetworkApisInSource()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string srcDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\src"));
            if (!Directory.Exists(srcDir))
            {
                srcDir = Path.GetFullPath(Path.Combine(baseDir, @"..\src"));
            }
            if (!Directory.Exists(srcDir))
            {
                srcDir = Path.GetFullPath("src");
            }
            AssertTrue(Directory.Exists(srcDir), "Could not locate src directory for audit check: " + srcDir);

            string[] forbiddenTokens = new string[]
            {
                "System.Net",
                "HttpClient",
                "WebClient",
                "HttpWebRequest",
                "TcpClient",
                "UdpClient",
                "Socket(",
                "ws2_32",
                "wininet",
                "winhttp"
            };

            string[] csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories);
            AssertTrue(csFiles.Length >= 10, "Expected at least 10 source files");

            for (int i = 0; i < csFiles.Length; i++)
            {
                // SecurityAuditDialog explains what APIs are NOT used in its UI text
                if (Path.GetFileName(csFiles[i]).Equals("SecurityAuditDialog.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string content = File.ReadAllText(csFiles[i]);
                for (int j = 0; j < forbiddenTokens.Length; j++)
                {
                    AssertTrue(
                        content.IndexOf(forbiddenTokens[j], StringComparison.OrdinalIgnoreCase) < 0,
                        "Forbidden network API token '" + forbiddenTokens[j] + "' found in " + csFiles[i]);
                }
            }
        }

        private static void TestHookInstallAndUninstall()
        {
            WindowService windowService = new WindowService();
            InputSenderService inputSender = new InputSenderService();
            using (KeyboardHookService hook = new KeyboardHookService(windowService, inputSender))
            {
                AppConfig cfg = AppConfig.CreateDefault();
                hook.Start(cfg);
                AssertTrue(hook.IsHookInstalled, "WH_KEYBOARD_LL hook should be installed after Start()");
                AssertEqual(BinderStatus.Active, hook.Status, "Status should be Active after Start()");

                hook.Stop();
                AssertTrue(!hook.IsHookInstalled, "WH_KEYBOARD_LL hook should be uninstalled after Stop()");
                AssertEqual(BinderStatus.Stopped, hook.Status, "Status should be Stopped after Stop()");
            }
        }

        private static void TestMainFormInitialization()
        {
            string tempCfg = Path.Combine(Path.GetTempPath(), "SafeKeyBinder_UITest_" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                ConfigService cfgSvc = new ConfigService(tempCfg);
                WindowService winSvc = new WindowService();
                InputSenderService senderSvc = new InputSenderService();
                using (KeyboardHookService hookSvc = new KeyboardHookService(winSvc, senderSvc))
                using (AutoSequenceService autoSvc = new AutoSequenceService(winSvc, senderSvc, hookSvc))
                using (MainForm form = new MainForm(cfgSvc, winSvc, senderSvc, hookSvc, autoSvc))
                {
                    form.CreateControl();
                    AssertTrue(form.Controls.Count > 0, "MainForm should create child controls");
                    AssertTrue(File.Exists(tempCfg), "Config file should be created on first load");
                }
            }
            finally
            {
                if (File.Exists(tempCfg)) File.Delete(tempCfg);
            }
        }

        private static void TestSequenceGeneratorLetters()
        {
            AutoSequenceConfig cfg = new AutoSequenceConfig();
            cfg.PatternMode = SequencePatternMode.LetterRange;
            cfg.PrefixText = "77";
            cfg.SuffixText = string.Empty;
            cfg.LetterStart = "a";
            cfg.LetterEnd = "z";

            SequenceGenerationResult r1 = SequenceGenerator.Generate(cfg);
            AssertTrue(r1.Success, "77a..77z should succeed: " + r1.ErrorMessage);
            AssertEqual(26, r1.Items.Count, "77a..77z should produce 26 items");
            AssertEqual("77a", r1.Items[0], "First item mismatch");
            AssertEqual("77z", r1.Items[25], "Last item mismatch");

            cfg.PrefixText = "";
            cfg.LetterStart = "aa";
            cfg.LetterEnd = "az";
            SequenceGenerationResult r2 = SequenceGenerator.Generate(cfg);
            AssertTrue(r2.Success, "aa..az should succeed");
            AssertEqual(26, r2.Items.Count, "aa..az count mismatch");
            AssertEqual("aa", r2.Items[0], "First 2-letter mismatch");
            AssertEqual("az", r2.Items[25], "Last 2-letter mismatch");

            cfg.LetterStart = "A";
            cfg.LetterEnd = "C";
            SequenceGenerationResult r3 = SequenceGenerator.Generate(cfg);
            AssertTrue(r3.Success, "A..C should preserve uppercase");
            AssertEqual("B", r3.Items[1], "Uppercase letter mismatch");
        }

        private static void TestSequenceGeneratorNumbersAndValidation()
        {
            AutoSequenceConfig cfg = new AutoSequenceConfig();
            cfg.PatternMode = SequencePatternMode.NumberRange;
            cfg.PrefixText = "world";
            cfg.NumberStart = 1;
            cfg.NumberEnd = 5;
            cfg.NumberPadDigits = 3;

            SequenceGenerationResult r1 = SequenceGenerator.Generate(cfg);
            AssertTrue(r1.Success, "Number range should succeed");
            AssertEqual(5, r1.Items.Count, "Number count mismatch");
            AssertEqual("world001", r1.Items[0], "Zero padding mismatch");
            AssertEqual("world005", r1.Items[4], "Last number mismatch");

            // Invalid reversed letter range
            cfg.PatternMode = SequencePatternMode.LetterRange;
            cfg.LetterStart = "z";
            cfg.LetterEnd = "a";
            SequenceGenerationResult invalid = SequenceGenerator.Generate(cfg);
            AssertTrue(!invalid.Success, "Reversed range z..a should fail validation");

            // Invalid mismatched length
            cfg.LetterStart = "a";
            cfg.LetterEnd = "zz";
            SequenceGenerationResult invalidLen = SequenceGenerator.Generate(cfg);
            AssertTrue(!invalidLen.Success, "Mismatched length a..zz should fail validation");
        }

        private static void TestSequenceFormatCommand()
        {
            AssertEqual("warp 77a", SequenceGenerator.FormatCommand("warp {item}", "77a"), "{item} placeholder replacement");
            AssertEqual("/warp 77b", SequenceGenerator.FormatCommand("/warp ", "77b"), "Append when no placeholder");
            AssertEqual("77c", SequenceGenerator.FormatCommand(string.Empty, "77c"), "Empty template returns item directly");
        }

        private static void TestAutoSequenceConfigRoundtrip()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "SafeKeyBinder_SeqTest_" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                ConfigService svc = new ConfigService(tempFile);
                AppConfig cfg = AppConfig.CreateDefault();
                cfg.AutoSequence.PrefixText = "farm";
                cfg.AutoSequence.LetterStart = "a";
                cfg.AutoSequence.LetterEnd = "m";
                cfg.AutoSequence.IntervalMs = 4200;
                cfg.AutoSequence.PrefixDelayMs = 150;
                cfg.AutoSequence.RequireTargetWindowFocus = true;
                cfg.AutoSequence.TargetWindowKeyword = "Roblox";

                svc.Save(cfg);
                AppConfig loaded = svc.Load();

                AssertEqual("farm", loaded.AutoSequence.PrefixText, "AutoSequence.PrefixText mismatch");
                AssertEqual("m", loaded.AutoSequence.LetterEnd, "AutoSequence.LetterEnd mismatch");
                AssertEqual(4200, loaded.AutoSequence.IntervalMs, "AutoSequence.IntervalMs mismatch");
                AssertEqual(150, loaded.AutoSequence.PrefixDelayMs, "AutoSequence.PrefixDelayMs mismatch");
                AssertEqual(true, loaded.AutoSequence.RequireTargetWindowFocus, "AutoSequence.RequireTargetWindowFocus mismatch");
                AssertEqual("Roblox", loaded.AutoSequence.TargetWindowKeyword, "AutoSequence.TargetWindowKeyword mismatch");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
