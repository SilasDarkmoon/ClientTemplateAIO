using UnityEngine;
using UnityEngine.UI;

namespace UnityEngineEx
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public class ImageMaterialChooser : MonoBehaviour, IMaterialModifier
    {
        public Material NormMaterial;
        public Material ETC1Material;

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (baseMaterial == Image.defaultGraphicMaterial)
            {
                return NormMaterial ?? baseMaterial;
            }
            else if (baseMaterial == Image.defaultETC1GraphicMaterial)
            {
                return ETC1Material ?? baseMaterial;
            }
            else
            {
                return baseMaterial;
            }
        }
    }
}