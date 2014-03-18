using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to him, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public static class PartExtensions
    {
        public static List<Part> GetAllChildren(this Part part)
        {
            if (part.children.Count > 0)
            {
                List<Part> children = new List<Part>(part.children);
                part.children.ForEach(p => children.AddRange(p.GetAllChildren()));
                return children;
            }
            return new List<Part>();
        }
    }

    public class ModuleTestWeight : PartModule
    {
        
        #region KSPFields
        [KSPField]
        public float maxMass = 100f;
        [KSPField]
        public float minMass = 0.01f;
        [KSPField]
        public float maxHeight = 5f;
        [KSPField]
        public float minHeight = 0.2f;
        [KSPField(isPersistant = true)]
        public float baseDiameter = 1.25f;
        [KSPField(isPersistant = true, guiActive = true, guiFormat = "0.###", guiName = "Current mass", guiUnits = "t", guiActiveEditor = true)]
        public float currentMass = 0;
        #endregion

        #region Persistent values
        [KSPField(isPersistant = true)]
        private string mass = string.Empty;
        [KSPField(isPersistant = true)]
        private int size = 1;
        [KSPField(isPersistant = true)]
        private float height = 1f, top = 0, bottom = 0;
        [KSPField(isPersistant = true)]
        private float currentBottom = 0, currentTop = 0;
        [KSPField(isPersistant = true)]
        public bool initiated = false;
        #endregion

        #region Propreties
        private GUIStyle RedLabel
        {
            get
            {
                GUIStyle style = new GUIStyle(skins.label);
                style.normal.textColor = XKCDColors.Red;
                style.hover.textColor = XKCDColors.Red;
                return style;
            }
        }
        #endregion

        #region Fields
        private int Id = Guid.NewGuid().GetHashCode();
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
        private void UpdateSize()
        {
            float radialFactor = baseRadial * width;
            float heightFactor = baseHeight * height;
            float originalX = this.part.transform.GetChild(0).localScale.x;
            float originalY = this.part.transform.GetChild(0).localScale.y;
            this.part.transform.GetChild(0).localScale = new Vector3(radialFactor, heightFactor, radialFactor);
            if ((HighLogic.LoadedSceneIsEditor && this.part == EditorLogic.SortedShipList[0]) || (HighLogic.LoadedSceneIsFlight && this.vessel.rootPart == this.part))
            {
                if (part.findAttachNode("top") != null)
                {
                    AttachNode topNode = part.findAttachNode("top");
                    float originalTop = topNode.position.y;
                    topNode.position.y = top * height;
                    currentTop = topNode.position.y;
                    if (topNode.attachedPart != null)
                    {
                        float topDifference = currentTop - originalTop;
                        topNode.attachedPart.transform.Translate(0, topDifference, 0, part.transform);
                        if (part.findAttachNode("top").attachedPart.GetAllChildren().Count > 0)
                        {
                            topNode.attachedPart.GetAllChildren().ForEach(p => p.transform.Translate(0, topDifference, 0, part.transform));
                        }
                    }
                }
                if (part.findAttachNode("bottom") != null && part.findAttachNode("bottom").attachedPart != null)
                {
                    AttachNode bottomNode = part.findAttachNode("bottom");
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = bottom * height;
                    currentBottom = bottomNode.position.y;
                    if (bottomNode.attachedPart != null)
                    {
                        float bottomDifference = currentBottom - originalBottom;
                        bottomNode.attachedPart.transform.Translate(0, bottomDifference, 0, part.transform);
                        if (part.findAttachNode("bottom").attachedPart.GetAllChildren().Count > 0)
                        {
                            bottomNode.attachedPart.GetAllChildren().ForEach(p => p.transform.Translate(0, bottomDifference, 0, part.transform));
                        }
                    }
                }
            }
            else if (part.findAttachNode("bottom") != null && part.findAttachNode("bottom").attachedPart != null && part.parent != null && part.findAttachNode("bottom").attachedPart == part.parent)
            {
                AttachNode bottomNode = part.findAttachNode("bottom");
                float originalBottom = bottomNode.position.y;
                bottomNode.position.y = bottom * height;
                currentBottom = bottomNode.position.y;
                float bottomDifference = currentBottom - originalBottom;
                part.transform.Translate(0, -bottomDifference, 0, part.transform);
                if (part.findAttachNode("top") != null)
                {
                    AttachNode topNode = part.findAttachNode("top");
                    float originalTop = topNode.position.y;
                    topNode.position.y = top * height;
                    currentTop = topNode.position.y;
                    float topDifference = currentTop - originalTop;
                    if (part.GetAllChildren().Count > 0) { part.GetAllChildren().ForEach(p => p.transform.Translate(0, -(bottomDifference - topDifference), 0, part.transform)); }
                }
            }
            else if (part.findAttachNode("top") != null && part.findAttachNode("top").attachedPart != null && part.parent != null && part.findAttachNode("top").attachedPart == part.parent)
            {
                AttachNode topNode = part.findAttachNode("top");
                float originalTop = topNode.position.y;
                topNode.position.y = top * height;
                currentTop = topNode.position.y;
                float topDifference = currentTop - originalTop;
                part.transform.Translate(0, -topDifference, 0, part.transform);
                if (part.findAttachNode("bottom") != null)
                {
                    AttachNode bottomNode = part.findAttachNode("bottom");
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = bottom * height;
                    currentBottom = bottomNode.position.y;
                    float bottomDifference = currentBottom - originalBottom;
                    if (part.GetAllChildren().Count > 0) { part.GetAllChildren().ForEach(p => p.transform.Translate(0, -(topDifference - bottomDifference), 0, part.transform)); }
                }
            }


            if (part.children.Count(p => p.attachMode == AttachModes.SRF_ATTACH) > 0)
            {
                float xScale = this.part.transform.GetChild(0).localScale.x / originalX;
                float yScale = this.part.transform.GetChild(0).localScale.y / originalY;
                List<Part> surfaceAttached = new List<Part>(part.children.Where(p => p.attachMode == AttachModes.SRF_ATTACH));
                surfaceAttached.Where(p => p.GetAllChildren().Count > 0).ToList().ForEach(p => surfaceAttached.AddRange(p.GetAllChildren()));
                foreach (Part p in surfaceAttached)
                {
                    Vector3 v = p.transform.position - part.transform.position;
                    p.transform.Translate(v.x * (xScale - 1), v.y * (yScale - 1), v.z * (xScale - 1), part.transform);
                }
            }
        }

        private float GetSize(int id)
        {
            return this.sizes.First(pair => pair.Key == id).Value;
        }

        private int GetId(float size)
        {
            return this.sizes.First(pair => pair.Value == size).Key;
        }

        private bool CanParse(string text)
        {
            double value;
            return double.TryParse(text, out value);
        }

        private bool CheckRange(float f, float min, float max)
        {
            return f > min && f <= max;
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorLogic.SortedShipList[0] == this.part || this.part.parent != null) { UpdateSize(); }
                this.currentMass = this.part.mass;
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
                    this.mass = this.part.mass.ToString();
                    try
                    {
                        size = GetId(this.baseDiameter);
                    }
                    catch (Exception)
                    {
                        Debug.LogError("[NRAP]: Invalid base diameter.");
                        size = 1;
                        baseDiameter = 1.25f;
                    }
                    if (this.part.findAttachNode("top") != null) { top = this.part.findAttachNode("top").originalPosition.y; }
                    if (this.part.findAttachNode("bottom") != null) { bottom = this.part.findAttachNode("bottom").originalPosition.y; }
                    currentTop = top;
                    currentBottom = bottom;
                }
                this.window = new Rect(200, 200, 300, 200);
                if (this.part.findAttachNode("top") != null) { this.part.findAttachNode("top").originalPosition.y = currentTop; }
                if (this.part.findAttachNode("bottom") != null) { this.part.findAttachNode("bottom").originalPosition.y = currentBottom; }
            }
            baseHeight = this.part.transform.GetChild(0).localScale.y;
            baseRadial = this.part.transform.GetChild(0).localScale.x;
            width = GetSize(size) / baseDiameter;
            float.TryParse(mass, out this.part.mass);
            if (HighLogic.LoadedSceneIsFlight)
            {
                UpdateSize();
                this.currentMass = this.part.mass;
            }
        }

        public override string GetInfo()
        {
            string infoList = String.Format("Mass range: {0} - {1}t\n", minMass, maxMass);
            infoList += String.Format("Height multiplier range: {0} - {1}\n", minHeight, maxHeight);
            infoList += String.Format("Base diameter: {0}m", baseDiameter);
            return infoList;
        }
        #endregion

        #region GUI
        private void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsEditor) { return; }
            if (this.visible)
            {
                this.window = GUILayout.Window(this.Id, this.window, Window, "NRAP Test Weight", skins.window);
            }
        }

        private void Window(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            if (CanParse(mass) && CheckRange(float.Parse(mass), minMass, maxMass)) { GUILayout.Label("Mass (t):", skins.label); }
            else { GUILayout.Label("Mass (t):", RedLabel); }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            mass = GUILayout.TextField(mass, 10, skins.textField, GUILayout.Width(125));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            if (GUILayout.Button("Apply", skins.button, GUILayout.Width(60))) { if (CheckRange(float.Parse(mass), minMass, maxMass)) { float.TryParse(mass, out this.part.mass); } }
            GUILayout.EndHorizontal();
            GUILayout.Label("Current mass: " + this.part.mass.ToString() + "t", skins.label);
            GUILayout.Space(10);
            GUILayout.Label("Diameter (m): " + GetSize(size), skins.label);
            size = (int)GUILayout.HorizontalSlider(size, 0, 4, skins.horizontalSlider, skins.horizontalSliderThumb);
            width = GetSize(size) /baseDiameter;
            GUILayout.Label("Height multiplier: " + height, skins.label);
            height = GUILayout.HorizontalSlider(height, minHeight, maxHeight, skins.horizontalSlider, skins.horizontalSliderThumb);         
            GUILayout.Space(10);
            if (GUILayout.Button("Close", skins.button)) { this.visible = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        #endregion
    }
}
