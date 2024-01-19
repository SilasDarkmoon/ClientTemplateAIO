#if UNITY_2022_1_OR_NEWER
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
	sealed class UniversalFullScreenSubTargetEx : SubTargetExBase<UniversalTarget, UniversalFullscreenSubTarget>, IRequiresData<FullscreenData>, IHasMetadata
	{
		public FullscreenData data { get => _Inner.fullscreenData; set => _Inner.fullscreenData = value; }

		public string identifier => _Inner.identifier;

		public ScriptableObject GetMetadataObject(GraphDataReadOnly graph)
		{
			return _Inner.GetMetadataObject(graph);
		}
	}
}
#endif
