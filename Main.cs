using ManagedCommon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Wox.Plugin;

namespace Community.PowerToys.Run.Plugin.ColorConverter
{
    /// <summary>
    /// 颜色格式互转插件，支持 #RRGGBB、RRGGBB（大小写均可）、rgb(r,g,b)、21, 31, 41 （中英文逗号，空格分隔）等格式。
    /// </summary>
    public class Main : IPlugin, IContextMenu, IDisposable
    {
        public static string PluginID => "COLORCONVERTERPLUGIN";

        public string Name => "ColorConverter";

        public string Description => "十六进制与RGB颜色互转";

        private PluginInitContext Context { get; set; }

        private string IconPath { get; set; }

        private bool Disposed { get; set; }

        public List<Result> Query(Query query)
        {
            var search = query.Search?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return [];

            var results = new List<Result>();

            // #RRGGBB 或 #RGB
            if (TryParseHexColor(search, out var r, out var g, out var b))
            {
                string rgb = $"rgb({r}, {g}, {b})";
                results.Add(new Result
                {
                    QueryTextDisplay = search,
                    IcoPath = IconPath,
                    Title = rgb,
                    SubTitle = $"十六进制 {search} 转为 RGB",
                    ToolTipData = new ToolTipData("RGB", rgb),
                    Action = _ =>
                    {
                        Clipboard.SetDataObject(rgb);
                        return true;
                    },
                    ContextData = rgb,
                });
            }
            // rgb(r,g,b)
            else if (TryParseRgbColor(search, out r, out g, out b) || TryParseCommaRgb(search, out r, out g, out b))
            {
                string hex = $"#{r:X2}{g:X2}{b:X2}";
                results.Add(new Result
                {
                    QueryTextDisplay = search,
                    IcoPath = IconPath,
                    Title = hex,
                    SubTitle = $"RGB {search} 转为 十六进制",
                    ToolTipData = new ToolTipData("Hex", hex),
                    Action = _ =>
                    {
                        Clipboard.SetDataObject(hex);
                        return true;
                    },
                    ContextData = hex,
                });
            }

            return results;
        }

        public void Init(PluginInitContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(Context.API.GetCurrentTheme());
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is string colorStr)
            {
                return
                [
                    new ContextMenuResult
                    {
                        PluginName = Name,
                        Title = "复制到剪贴板 (Ctrl+C)",
                        FontFamily = "Segoe MDL2 Assets",
                        Glyph = "\xE8C8",
                        AcceleratorKey = Key.C,
                        AcceleratorModifiers = ModifierKeys.Control,
                        Action = _ =>
                        {
                            Clipboard.SetDataObject(colorStr);
                            return true;
                        },
                    }
                ];
            }

            return [];
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
                return;

            if (Context?.API != null)
                Context.API.ThemeChanged -= OnThemeChanged;

            Disposed = true;
        }

        private void UpdateIconPath(Theme theme) =>
            IconPath = theme == Theme.Light || theme == Theme.HighContrastWhite
                ? "Images/colorconverter.light.png"
                : "Images/colorconverter.dark.png";

        private void OnThemeChanged(Theme currentTheme, Theme newTheme) => UpdateIconPath(newTheme);

        // 解析 #RRGGBB 或 #RGB
        private static bool TryParseHexColor(string input, out int r, out int g, out int b)
        {
            r = g = b = 0;
            var hex = input.Trim().TrimStart('#');
            if (hex.Length == 3)
            {
                r = Convert.ToInt32(new string(hex[0], 2), 16);
                g = Convert.ToInt32(new string(hex[1], 2), 16);
                b = Convert.ToInt32(new string(hex[2], 2), 16);
                return true;
            }
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, null, out var rgb))
            {
                r = (rgb >> 16) & 0xFF;
                g = (rgb >> 8) & 0xFF;
                b = rgb & 0xFF;
                return true;
            }
            return false;
        }

        // 解析 rgb(r,g,b)
        private static bool TryParseRgbColor(string input, out int r, out int g, out int b)
        {
            r = g = b = 0;
            var match = Regex.Match(input, @"rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)", RegexOptions.IgnoreCase);
            if (match.Success
                && int.TryParse(match.Groups[1].Value, out r)
                && int.TryParse(match.Groups[2].Value, out g)
                && int.TryParse(match.Groups[3].Value, out b)
                && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            {
                return true;
            }
            return false;
        }

        // 解析 21, 31, 41 或中文逗号、空格分隔的格式
        private static bool TryParseCommaRgb(string input, out int r, out int g, out int b)
        {
            r = g = b = 0;
            // 支持英文逗号、中文逗号、空格等任意分隔
            var parts = Regex.Split(input.Trim(), @"[,\s，]+");
            if (parts.Length == 3
                && int.TryParse(parts[0], out r)
                && int.TryParse(parts[1], out g)
                && int.TryParse(parts[2], out b)
                && r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            {
                return true;
            }
            return false;
        }
    }
}