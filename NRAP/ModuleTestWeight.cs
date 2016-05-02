using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to him, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public class ModuleTestWeight : PartModule, IPartMassModifier, IPartCostModifier
    {      
        #region KSPFields
        [KSPField]
        public float maxMass = 100;

        [KSPField(isPersistant = true)]
        public float minMass = 0.01f;

        [KSPField]
        public float maxHeight = 5;

        [KSPField]
        public float minHeight = 0.2f;

        [KSPField]
        public float weightCost = 0.1f;

        [KSPField(isPersistant = true)]
        public float baseDiameter = 1.25f;

        [KSPField(isPersistant = true, guiActive = true, guiFormat = "0.###", guiName = "Total mass", guiUnits = "t", guiActiveEditor = true)]
        public float currentMass;

        [KSPField(isPersistant = true)]
        public string mass = string.Empty;

        [KSPField(isPersistant = true)]
        private int size = 1;

        [KSPField(isPersistant = true)]
        public float deltaMass;

        [KSPField(isPersistant = true)]
        private float height = 1, top, bottom;

        [KSPField(isPersistant = true)]
        private float currentBottom, currentTop;

        [KSPField(isPersistant = true)]
        public bool initiated;
        #endregion

        #region Fields
        private readonly int id = Guid.NewGuid().GetHashCode();
        private Rect window , drag;
        private bool visible;
        private readonly Dictionary<int, float> sizes = new Dictionary<int, float>(5)
        {
            { 0, 0.625f },
            { 1, 1.25f },
            { 2, 2.5f },
            { 3, 3.75f },
            { 4, 5 }
        };
        private float width, baseHeight, baseRadial;
        #endregion

        #region Part GUI
        [KSPEvent(active = true, guiActive = false, guiActiveEditor = true, guiName = "Toggle window")]
        public void GUIToggle()
        {
            CloseOpenedWindow();
            this.visible = !this.visible;
        }
        #endregion

        #region Methods
        private bool CheckParentNode(AttachNode node)
        {
            return node.attachedPart != null && node.attachedPart == this.part?.parent;
        }

        private void UpdateSize()
        {
            AttachNode topNode, bottomNode;
            bool hasTopNode = this.part.TryGetAttachNodeById("top", out topNode);
            bool hasBottomNode = this.part.TryGetAttachNodeById("bottom", out bottomNode);
            float radialFactor = this.baseRadial * this.width;
            float heightFactor = this.baseHeight * this.height;
            Transform root = this.part.transform.GetChild(0);
            float originalX = root.localScale.x;
            float originalY = root.localScale.y;
            root.localScale = new Vector3(radialFactor, heightFactor, radialFactor);

            //If part is root part
            if ((HighLogic.LoadedSceneIsEditor && this.part == EditorLogic.SortedShipList[0]) || (HighLogic.LoadedSceneIsFlight && this.vessel.rootPart == this.part))
            {
                if (hasTopNode)
                {
                    float originalTop = topNode.position.y;
                    topNode.position.y = this.top * this.height;
                    this.currentTop = topNode.position.y;
                    if (topNode.attachedPart != null)
                    {
                        float topDifference = this.currentTop - originalTop;
                        topNode.attachedPart.transform.Translate(0, topDifference, 0, this.part.transform);
                    }
                }
                if (hasBottomNode)
                {
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = this.bottom * this.height;
                    this.currentBottom = bottomNode.position.y;
                    if (bottomNode.attachedPart != null)
                    {
                        float bottomDifference = this.currentBottom - originalBottom;
                        bottomNode.attachedPart.transform.Translate(0, bottomDifference, 0, this.part.transform);
                    }
                }
            }

            //If parent part is attached to bottom node
            else if (hasBottomNode && CheckParentNode(bottomNode))
            {
                float originalBottom = bottomNode.position.y;
                bottomNode.position.y = this.bottom * this.height;
                this.currentBottom = bottomNode.position.y;
                float bottomDifference = this.currentBottom - originalBottom;
                this.part.transform.Translate(0, -bottomDifference, 0, this.part.transform);
                if (hasTopNode)
                {
                    float originalTop = topNode.position.y;
                    topNode.position.y = this.top * this.height;
                    this.currentTop = topNode.position.y;
                    float topDifference = this.currentTop - originalTop;
                    topNode.attachedPart.transform.Translate(0, -(bottomDifference - topDifference), 0, this.part.transform);
                }
            }

            //If parent part is attached to top node
            else if (hasTopNode && CheckParentNode(topNode))
            {
                float originalTop = topNode.position.y;
                topNode.position.y = this.top * this.height;
                this.currentTop = topNode.position.y;
                float topDifference = this.currentTop - originalTop;
                this.part.transform.Translate(0, -topDifference, 0, this.part.transform);
                if (hasBottomNode)
                {
                    float originalBottom = bottomNode.position.y;
                    bottomNode.position.y = this.bottom * this.height;
                    this.currentBottom = bottomNode.position.y;
                    float bottomDifference = this.currentBottom - originalBottom;
                    bottomNode.attachedPart.transform.Translate(0, -(topDifference - bottomDifference), 0, this.part.transform);
                }
            }

            //Surface attached parts
            if (this.part.children.Any(p => p.attachMode == AttachModes.SRF_ATTACH))
            {
                float scaleX = root.localScale.x / originalX;
                float scaleY = root.localScale.y / originalY;
                foreach (Part child in this.part.children)
                {
                    if (child.attachMode == AttachModes.SRF_ATTACH)
                    {
                        // vv  From https://github.com/Biotronic/TweakScale/blob/master/Scale.cs#L403  vv
                        Vector3 vX = (child.transform.localPosition + (child.transform.localRotation * child.srfAttachNode.position)) - this.part.transform.position;

                        Vector3 vY = child.transform.position - this.part.transform.position;
                        child.transform.Translate(vX.x * (scaleX - 1), vY.y * (scaleY - 1), vX.z * (scaleX - 1), this.part.transform);
                    }
                }
            }

            //Node size
            int nodeSize = Math.Min(this.size, 3);
            if (hasBottomNode) { bottomNode.size = nodeSize; }
            if (hasTopNode) { topNode.size = nodeSize; }
            if (HighLogic.LoadedSceneIsEditor) { GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship); }
            else if (HighLogic.LoadedSceneIsFlight) { StartCoroutine(UpdateDragCube()); }
        }

        private float GetSize(int id) => this.sizes[id];

        private int GetID(float size) => this.sizes.First(p => p.Value == size).Key;

        private IEnumerator<YieldInstruction> UpdateDragCube()
        {
            while (!FlightGlobals.ready || this.part.packed || !this.vessel.loaded)
            {
                yield return new WaitForFixedUpdate();
            }

            this.part.DragCubes.ClearCubes();
            this.part.DragCubes.Cubes.Add(DragCubeSystem.Instance.RenderProceduralDragCube(this.part));
            this.part.DragCubes.ResetCubeWeights();
        }

        private void Window(int id)
        {
            GUI.DragWindow(this.drag);
            GUILayout.BeginVertical();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Dry mass (t):", NRAPUtils.CanParse(this.mass) && NRAPUtils.CheckRange(float.Parse(this.mass), this.minMass, this.maxMass) ? GUI.skin.label : NRAPUtils.RedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            this.mass = GUILayout.TextField(this.mass, 10, GUILayout.Width(125));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                float m;
                if (float.TryParse(this.mass, out m) && NRAPUtils.CheckRange(m, this.minMass, this.maxMass))
                {
                    this.deltaMass = m - this.part.partInfo.partPrefab.mass;
                    this.currentMass = this.part.TotalMass();
                    GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                }
            }
            GUILayout.EndHorizontal();

            StringBuilder builder = new StringBuilder().AppendFormat("\nCurrent total mass: {0}t ({1}t dry - {2}t resources)\n", this.part.TotalMass(), this.part.mass, this.part.GetResourceMass());
            builder.AppendFormat("Test weight cost: {0}f (total: {1}f)", GetModuleCost(0, 0), this.part.TotalCost());
            GUILayout.Label(builder.ToString());
            GUILayout.Space(10);

            GUILayout.Label($"Diameter (m): {{ {GetSize(this.size)}");
            this.size = (int)GUILayout.HorizontalSlider(this.size, 0, 4);
            this.width = GetSize(this.size) / this.baseDiameter;

            GUILayout.Label($"Height multiplier: {this.height.ToString("0.000")}");
            this.height = GUILayout.HorizontalSlider(this.height, this.minHeight, this.maxHeight);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to defaults", GUILayout.Width(150)))
            {
                this.deltaMass = 0;
                this.mass = this.part.partInfo.partPrefab.mass.ToString();
                this.currentMass = this.part.TotalMass();
                this.size = GetID(this.baseDiameter);
                this.width = 1;
                this.height = 1;
            }

            if (GUILayout.Button("Close", GUILayout.Width(150))) { this.visible = false; }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit) => this.part.mass * this.weightCost;

        public ModifierChangeWhen GetModuleCostChangeWhen() => ModifierChangeWhen.FIXED;

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit) => this.deltaMass;

        public ModifierChangeWhen GetModuleMassChangeWhen() => ModifierChangeWhen.FIXED;
        #endregion

        #region Static methods
        public void CloseOpenedWindow()
        {
            foreach (Part p in EditorLogic.SortedShipList)
            {
                foreach (PartModule pm in p.Modules)
                {
                    ModuleTestWeight tw = pm as ModuleTestWeight;
                    if (tw != null && tw.visible)
                    {
                        tw.visible = false;
                        return;
                    }
                }
            }
        }
        #endregion

        #region Functions
        private void Update()
        {
            if (CompatibilityChecker.IsAllCompatible() && HighLogic.LoadedSceneIsEditor && (EditorLogic.SortedShipList[0] == this.part || this.part.parent != null))
            {
                UpdateSize();
            }
        }

        private void OnGUI()
        {
            if (CompatibilityChecker.IsAllCompatible() && HighLogic.LoadedSceneIsEditor && this.visible)
            {
                GUI.skin = HighLogic.Skin;
                this.window = GUILayout.Window(this.id, this.window, Window, "NRAP Test Weight " + NRAPUtils.AssemblyVersion);
            }
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            if (!CompatibilityChecker.IsAllCompatible() || (!HighLogic.LoadedSceneIsFlight && !HighLogic.LoadedSceneIsEditor)) { return; }
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (!this.initiated)
                {
                    this.initiated = true;
                    this.mass = this.part.mass.ToString();
                    try
                    {
                        this.size = GetID(this.baseDiameter);
                    }
                    catch (Exception)
                    {
                        Debug.LogError("[NRAP]: Invalid base diameter.");
                        this.size = 1;
                        this.baseDiameter = 1.25f;
                    }

                    if (this.part.findAttachNode("top") != null) { this.top = this.part.findAttachNode("top").originalPosition.y; }
                    if (this.part.findAttachNode("bottom") != null) { this.bottom = this.part.findAttachNode("bottom").originalPosition.y; }
                    this.currentTop = this.top;
                    this.currentBottom = this.bottom;
                    if (this.minMass <= 0) { this.minMass = 0.01f; }
                }
                this.window = new Rect(200, 200, 300, 200);
                this.drag = new Rect(0, 0, 300, 30);
                if (this.part.findAttachNode("top") != null) { this.part.findAttachNode("top").originalPosition.y = this.currentTop; }
                if (this.part.findAttachNode("bottom") != null) { this.part.findAttachNode("bottom").originalPosition.y = this.currentBottom; }
            }
            this.baseHeight = this.part.transform.GetChild(0).localScale.y;
            this.baseRadial = this.part.transform.GetChild(0).localScale.x;
            this.width = GetSize(this.size) / this.baseDiameter;
            this.currentMass = this.part.partInfo.partPrefab.mass + this.deltaMass;

            if (HighLogic.LoadedSceneIsFlight) { UpdateSize(); }
        }

        public override string GetInfo()
        {
            if (!CompatibilityChecker.IsAllCompatible()) { return string.Empty; }

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Mass range: {0} - {1}t\n", this.maxMass, this.minMass);
            builder.AppendFormat("Height multiplier range: {0} - {1}\n", this.minHeight, this.maxHeight);
            builder.AppendFormat("Base diameter: {0}m\n", this.baseDiameter);
            builder.Append("Base diameter range: 0.625m, 1.25m, 2.5m, 3.75m, 5m");
            return builder.ToString();
        }
        #endregion
    }
}
