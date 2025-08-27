using System;

namespace UABS;

public static class ColorHelper
{
    public static string Saturate(string hex, double factor = 1.2)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) throw new ArgumentException("Only 6-digit RGB hex is supported.");

        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);

        // Convert to 0..1
        double rNorm = r / 255.0;
        double gNorm = g / 255.0;
        double bNorm = b / 255.0;

        // Compute average intensity
        double avg = (rNorm + gNorm + bNorm) / 3.0;

        // Boost each channel away from average
        rNorm = avg + (rNorm - avg) * factor;
        gNorm = avg + (gNorm - avg) * factor;
        bNorm = avg + (bNorm - avg) * factor;

        // Clamp to [0,1]
        rNorm = Math.Min(1.0, Math.Max(0.0, rNorm));
        gNorm = Math.Min(1.0, Math.Max(0.0, gNorm));
        bNorm = Math.Min(1.0, Math.Max(0.0, bNorm));

        return $"#{(int)(rNorm*255):X2}{(int)(gNorm*255):X2}{(int)(bNorm*255):X2}";
    }
}