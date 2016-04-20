using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using Version = System.Version;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to Kotysoft, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public static class NRAPUtils
    {
        #region Propreties
        private static readonly string assemblyVersion;
        /// <summary>
        /// Returns the assembly informational version of the mod
        /// </summary>
        public static string AssemblyVersion
        {
            get { return assemblyVersion; }
        }

        private static readonly GUIStyle redLabel;
        /// <summary>
        /// A red GUI label
        /// </summary>
        public static GUIStyle RedLabel
        {
            get { return redLabel; }
        }
        #endregion

        #region Constructors
        static NRAPUtils()
        {
            Version version = new Version(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            if (version.Revision == 0)
            {
                if (version.Build == 0) { assemblyVersion = "v" + version.ToString(2); }
                else { assemblyVersion = "v" + version.ToString(3); }
            }
            else { assemblyVersion = "v" + version; }

            redLabel = new GUIStyle(HighLogic.Skin.label)
            {
                normal = { textColor = XKCDColors.Red },
                hover = { textColor = XKCDColors.Red }
            };
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
