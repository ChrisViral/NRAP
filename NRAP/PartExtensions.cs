using System.Collections.Generic;

/* NRAP Test Weights is licensed under CC-BY-SA. All Rights for the original mod and for attribution 
 * go to Kotysoft, excepted for this code, which is the work of Christophe Savard (stupid_chris).*/

namespace NRAP
{
    public static class PartExtensions
    {
        #region Methods
        /// <summary>
        /// Returns the dry mass and resource mass of the part
        /// </summary>
        public static float TotalMass(this Part part)
        {
            return part.physicalSignificance != Part.PhysicalSignificance.NONE ? part.mass + part.GetResourceMass() : 0f;
        }

        /// <summary>
        /// Returns the full cost of this part
        /// </summary>
        public static float TotalCost(this Part part)
        {
            return part.partInfo.cost + part.GetModuleCosts();
        }

        /// <summary>
        /// Sees if the part has the given AttachNode and stores it in the out value. Returns false if the node is null.
        /// </summary>
        /// <param name="nodeId">Name of the node to find</param>
        /// <param name="node">Value to store the result into</param>
        public static bool TryGetAttachNodeById(this Part part, string nodeId, out AttachNode node)
        {
            node = part.findAttachNode(nodeId);
            return node != null;
        }
        #endregion
    }
}
