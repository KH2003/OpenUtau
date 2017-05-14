﻿using OpenUtau.Core.USTx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace OpenUtau.Core.Util
{
    class Utils
    {
        public static void SetExpresstionValue(UExpression expression, object obj)
        {
            if (expression is IntExpression)
            {
                expression.Data = (int)obj;
            }
            if (expression is FloatExpression)
            {
                expression.Data = (float)obj;
            }
        }
        public static void SetExpresstionValue(UExpression expression, string obj)
        {
            if (expression is IntExpression)
            {
                expression.Data = int.Parse(obj);
            }
            if (expression is FloatExpression)
            {
                expression.Data = float.Parse(obj);
            }
        }
    }

    public static class ColorsConverter
    {
        public static Color ToMediaColor(this System.Drawing.Color drawingColor) => 
            new Color() { R = drawingColor.R, G = drawingColor.G, B = drawingColor.B, A = drawingColor.A };

        public static System.Drawing.Color ToDrawingColor(this Color mediaColor) =>
            System.Drawing.Color.FromArgb(red: mediaColor.R, green: mediaColor.G, blue: mediaColor.B, alpha: mediaColor.A);
    }
}