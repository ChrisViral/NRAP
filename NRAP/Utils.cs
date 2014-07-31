using System.Reflection;
using System.Diagnostics;
using UnityEngine;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to Kotysoft, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public static class Utils
    {
        #region Propreties
        /// <summary>
        /// Returns the assembly informational version of the mod
        /// </summary>
        public static string assemblyVersion
        {
            get
            {
                System.Version version = new System.Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
                if (version.Revision == 0)
                {
                    if (version.Build == 0) { return "v" + version.ToString(2); }
                    return "v" + version.ToString(3);
                }
                return "v" + version.ToString();
            }
        }

        private static GUIStyle _redLabel = null;
        /// <summary>
        /// A red GUI label
        /// </summary>
        public static GUIStyle redLabel
        {
            get
            {
                if (_redLabel == null)
                {
                    GUIStyle style = new GUIStyle(HighLogic.Skin.label);
                    style.normal.textColor = XKCDColors.Red;
                    style.hover.textColor = XKCDColors.Red;
                    _redLabel = style;
                }
                return _redLabel;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Checks if the string can be parsed into a float
        /// </summary>
        /// <param name="text">String to parse</param>
        public static bool CanParse(string text)
        {
            double value;
            return double.TryParse(text, out value);
        }

        /// <summary>
        /// Checks if the given float is within the asked range
        /// </summary>
        /// <param name="f">Float to check</param>
        /// <param name="min">Minimum bound</param>
        /// <param name="max">Maximum bound</param>
        public static bool CheckRange(float f, float min, float max)
        {
            return f > min && f <= max;
        }
        #endregion
    }
}
