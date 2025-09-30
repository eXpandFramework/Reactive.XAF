using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Xpand.Extensions.Colors{
    public static partial class ColorExtensions{
        public static string[] DistinctColors(this int i){
            var colors = new List<Color>();
            var index = 0;
            while (colors.Count < i){
                var color = Color.FromArgb(GetRGB(index));
                if (!IsInvalidColor(color) && !color.IsSimilar( colors))
                    colors.Add(color.LightenColor());
                index++;
            }
            return colors.Select(c => c.ToHex()).ToArray();
        }

        static bool IsInvalidColor(Color color){
            var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            var nearWhite = luminance > 0.4;
            var nearBlack = luminance < 0.2;
            var nearGray = Math.Abs(color.R - color.G) < 10 && Math.Abs(color.G - color.B) < 10;
            return nearWhite || nearBlack || nearGray;
        }

        static int GetRGB(int index){
            var p = GetPattern(index);
            return (GetElement(p[0]) << 16) | (GetElement(p[1]) << 8) | GetElement(p[2]);
        }

        static int GetElement(int index){
            var value = index - 1;
            var v = 0;
            for (var i = 0; i < 8; i++){
                v |= (value & 1);
                v <<= 1;
                value >>= 1;
            }
            v >>= 1;
            return v & 0xFF;
        }

        static int[] GetPattern(int index){
            var n = (int)Math.Cbrt(index);
            index -= n * n * n;
            var p = new int[3];
            Array.Fill(p, n);
            if (index == 0) return p;
            index--;
            var v = index % 3;
            index /= 3;
            if (index < n){
                p[v] = index % n;
                return p;
            }

            index -= n;
            p[v] = index / n;
            p[++v % 3] = index % n;
            return p;
        }
    }
}