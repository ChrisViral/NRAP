using System.Linq;
using UnityEngine;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to him, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class NRAPIconManager : MonoBehaviour
    {
        #region Methods
        private void CorrectIcon()
        {
            PartCategorizer.Icon icon = PartCategorizer.Instance.GetIcon("R&D_node_icon_generic");
            PartCategorizer.Instance.filters.Find(f => f.button.categoryName == "Filter by Module").subcategories
                .Select(s => s.button).Single(b => b.categoryName == "Test Weight").SetIcon(icon);
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            GameEvents.onGUIEditorToolbarReady.Add(CorrectIcon);
        }
        #endregion
    }
}
