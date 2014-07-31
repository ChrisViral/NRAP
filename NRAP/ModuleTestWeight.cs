using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to him, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public class ModuleTestWeight : PartModule, IPartCostModifier
    {      
        #region KSPFields
        [KSPField]
        public float maxMass = 100f;
        [KSPField(isPersistant = true)]
        public float minMass = 0.01f;
        [KSPField]
        public float maxHeight = 5f;
        [KSPField]
        public float minHeight = 0.2f;
        [KSPField]
        public float weightCost = 0.1f;
        [KSPField(isPersistant = true)]
        public float baseDiameter = 1.25f;
        [KSPField(isPersistant = true, guiActive = true, guiFormat = "0.###", guiName = "Total mass", guiUnits = "t", guiActiveEditor = true)]
        public float currentMass = 0;
        #endregion

        #region Persistent values
        [KSPField(isPersistant = true)]
        private string mass = string.Empty;
        [KSPField(isPersistant = true)]
        private int size = 1;
        [KSPField(isPersistant = true)]
        public float baseMass = 0;
        [KSPField(isPersistant = true)]
        private float height = 1f, top = 0, bottom = 0;
        [KSPField(isPersistant = true)]
        private float currentBottom = 0, currentTop = 0;
        [KSPField(isPersistant = true)]
        public bool initiated = false;
        #endregion

        #region Fields
        private int id = Guid.NewGuid().GetHashCode();
        private Rect window = new Rect();
        private bool visible = false;
        private readonly Dictionary<int, float> sizes = new Dictionary<int, float>(5)
        {
            { 0, 0.625f },
            { 1, 1.25f },
            { 2, 2.5f },
            { 3, 3.75f },
            { 4, 5f }
        };
        private GUISkin skins = HighLogic.Skin;
        private float width = 0;
        private float baseHeight = 0, baseRadial = 0;
        #endregion

        #region Part GUI
        [KSPEvent(active = true, guiActiveEditor = true, guiName = "Toggle window")]
        public void GUIToggle()
        {
            if (EditorLogic.SortedShipList.Count(p => p.Modules.Contains("ModuleTestWeight")) > 0)
            {
                List<ModuleTestWeight> modules = new List<ModuleTestWeight>(EditorLogic.SortedShipList.Where(p => p.Modules.Contains("ModuleTestWeight")).Select(p => (ModuleTestWeight)p.Modules["ModuleTestWeight"]).Where(m => m != this));
                if (modules.Any(m => m.visible)) { modules.Find(m => m.visible).visible = false; }
            }
            this.visible = !this.visible;
        }
        #endregion

        #region Methods
        private bool CheckParentNode(AttachNode node)
        {
            return node.attachedPart != null && part.parent != null && node.attachedPart == part.parent;
        }

        private void UpdateSize()
        {
            AttachNode topNode = null, bottomNode = null;
            bool hasTopNode = part.TryGetAttachNodeById("top", out topNode);
            bool hasBottomNode = part.TryGetAttachNodeById("bottom", out bottomNode);
            float radialFactor = baseRadial * width;
            float heightFactor = baseHeight * height;
            float originalX = this.part.transform.GetChild(0).localScale.x;
            float originalY = this.part.transform.GetChild(0).localScale.y;
            this.part.transform.GetChild(0).localScale = new Vector3(radialFactor, heightFactor, radialFactor);

            //If part is root part
            if ((HighLogic.LoadedSceneIsEditor && this.part == EditorLogic.SortedShipList[0]) || (HighLogic.LoadedSceneIsFlight && this.vessel.rootPart == this.part))
            {
                if (hasTopNode)
                {
                    float originalTop = topNode.position.y;
                    topNode.position.y = top * height;
                    currentTop = topNode.position.y;
                    if (topNode.attachedPart != null)
                    {
                        float topDifference = currentTop - originalTop;
                        topNode.attachedPart.transform.Translate(0, topDifference, 0, part.transform);
                    }
                }
                if (hasBottomNode)
                {
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = bottom * height;
                    currentBottom = bottomNode.position.y;
                    if (bottomNode.attachedPart != null)
                    {
                        float bottomDifference = currentBottom - originalBottom;
                        bottomNode.attachedPart.transform.Translate(0, bottomDifference, 0, part.transform);
                    }
                }
            }

            //If parent part is attached to bottom node
            else if (hasBottomNode && CheckParentNode(bottomNode))
            {
                float originalBottom = bottomNode.position.y;
                bottomNode.position.y = bottom * height;
                currentBottom = bottomNode.position.y;
                float bottomDifference = currentBottom - originalBottom;
                part.transform.Translate(0, -bottomDifference, 0, part.transform);
                if (hasTopNode)
                {
                    float originalTop = topNode.position.y;
                    topNode.position.y = top * height;
                    currentTop = topNode.position.y;
                    float topDifference = currentTop - originalTop;
                    topNode.attachedPart.transform.Translate(0, -(bottomDifference - topDifference), 0, part.transform);
                }
            }

            //If parent part is attached to top node
            else if (hasTopNode && CheckParentNode(topNode))
            {
                float originalTop = topNode.position.y;
                topNode.position.y = top * height;
                currentTop = topNode.position.y;
                float topDifference = currentTop - originalTop;
                part.transform.Translate(0, -topDifference, 0, part.transform);
                if (hasBottomNode)
                {
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = bottom * height;
                    currentBottom = bottomNode.position.y;
                    float bottomDifference = currentBottom - originalBottom;
                    bottomNode.attachedPart.transform.Translate(0, -(topDifference - bottomDifference), 0, part.transform);
                }
            }

            //Surface attached parts
            if (part.children.Any(p => p.attachMode == AttachModes.SRF_ATTACH))
            {
                float scaleX = this.part.transform.GetChild(0).localScale.x / originalX;
                float scaleY = this.part.transform.GetChild(0).localScale.y / originalY;
                foreach (Part child in part.children)
                {
                    if (child.attachMode == AttachModes.SRF_ATTACH)
                    {
                        // vv  From https://github.com/Biotronic/TweakScale/blob/master/Scale.cs#L403  vv
                        Vector3 vX = (child.transform.localPosition + child.transform.localRotation * child.srfAttachNode.position) - part.transform.position;

                        Vector3 vY = child.transform.position - part.transform.position;
                        child.transform.Translate(vX.x * (scaleX - 1), vY.y * (scaleY - 1), vX.z * (scaleX - 1), part.transform);
                    }
                }
            }

            //Node size
            int nodeSize = Math.Min(size, 3);
            if (hasBottomNode) { bottomNode.size = nodeSize; }
            if (hasTopNode) { topNode.size = nodeSize; }
        }

        private float GetSize(int id)
        {
            return this.sizes[id];
        }

        private int GetId(float size)
        {
            return this.sizes.First(pair => pair.Value == size).Key;
        }

        public float GetModuleCost()
        {
            return this.part.mass * this.weightCost;
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (HighLogic.LoadedSceneIsEditor && (EditorLogic.SortedShipList[0] == this.part || this.part.parent != null))
            {
                UpdateSize();
            }
        }
        #endregion

        #region Overrides
        public override void OnStart(PartModule.StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor) { return; }
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (!initiated)
                {
                    initiated = true;
                    baseMass = this.part.mass;
                    this.mass = this.baseMass.ToString();
                    try
                    {
                        size = GetId(this.baseDiameter);
                    }
                    catch (Exception)
                    {
                        Debug.LogError("[NRAP]: Invalid base diameter.");
                        this.size = 1;
                        this.baseDiameter = 1.25f;
                    }
                    if (this.part.findAttachNode("top") != null) { top = this.part.findAttachNode("top").originalPosition.y; }
                    if (this.part.findAttachNode("bottom") != null) { bottom = this.part.findAttachNode("bottom").originalPosition.y; }
                    currentTop = top;
                    currentBottom = bottom;
                    if (minMass <= 0) { minMass = 0.01f; }
                }
                this.window = new Rect(200, 200, 300, 200);
                if (this.part.findAttachNode("top") != null) { this.part.findAttachNode("top").originalPosition.y = currentTop; }
                if (this.part.findAttachNode("bottom") != null) { this.part.findAttachNode("bottom").originalPosition.y = currentBottom; }
            }
            baseHeight = this.part.transform.GetChild(0).localScale.y;
            baseRadial = this.part.transform.GetChild(0).localScale.x;
            width = GetSize(size) / baseDiameter;
            float.TryParse(mass, out this.part.mass);
            this.currentMass = this.part.TotalMass();
            if (HighLogic.LoadedSceneIsFlight) { UpdateSize(); }
        }

        public override string GetInfo()
        {
            StringBuilder builder = new StringBuilder().AppendFormat("Mass range: {0} - {1}t\n", maxMass, minMass);
            builder.AppendFormat("Height multiplier range: {0} - {1}\n", minHeight, maxHeight);
            builder.AppendFormat("Base diameter: {0}m\n", baseDiameter);
            return builder.Append("Base diameter range: 0.625m, 1.25m, 2.5m, 3.75m, 5m").ToString();
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; }
            if (this.visible)
            {
                this.window = GUILayout.Window(this.id, this.window, Window, "NRAP Test Weight " + Utils.assemblyVersion, skins.window);
            }
        }

        private void Window(int id)
        {
            GUI.DragWindow(new Rect(0, 0, this.window.width, 30));
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            if (Utils.CanParse(mass) && Utils.CheckRange(float.Parse(mass), minMass, maxMass)) { GUILayout.Label("Dry mass (t):", skins.label); }
            else { GUILayout.Label("Dry mass (t):", Utils.redLabel); }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            mass = GUILayout.TextField(mass, 10, skins.textField, GUILayout.Width(125));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (GUILayout.Button("Apply", skins.button, GUILayout.Width(60)))
            {
                float m = 0;
                float.TryParse(mass, out m);
                if (Utils.CheckRange(m, minMass, maxMass))
                {
                    this.part.mass = m;
                    this.currentMass = this.part.TotalMass();
                }
            }
            GUILayout.EndHorizontal();

            StringBuilder builder = new StringBuilder().AppendFormat("\nCurrent total mass: {0}t ({1}t dry - {2}t resources)\n", this.part.TotalMass(), this.part.mass, this.part.GetResourceMass());
            builder.AppendFormat("Test weight cost: {0}f (total: {1}f)", GetModuleCost(), this.part.TotalCost());
            GUILayout.Label(builder.ToString(), skins.label);
            GUILayout.Space(10);

            GUILayout.Label("Diameter (m): " + GetSize(size), skins.label);
            size = (int)GUILayout.HorizontalSlider(size, 0, 4, skins.horizontalSlider, skins.horizontalSliderThumb);
            width = GetSize(size) /baseDiameter;

            GUILayout.Label("Height multiplier: " + height.ToString("0.000"), skins.label);
            height = GUILayout.HorizontalSlider(height, minHeight, maxHeight, skins.horizontalSlider, skins.horizontalSliderThumb);         
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to defaults", skins.button, GUILayout.Width(150)))
            {
                this.part.mass = this.baseMass;
                this.mass = this.baseMass.ToString();
                this.currentMass = this.part.TotalMass();
                size = GetId(baseDiameter);
                width = 1;
                height = 1;
            }

            if (GUILayout.Button("Close", skins.button, GUILayout.Width(150))) { this.visible = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion
    }
}
