using System.Collections.Generic;

namespace Nez.Tiled
{
	public interface ITmxLayer : ITmxElement
	{
		TmxMap Map { get; }
		float OffsetX { get; }
		float OffsetY { get; }
		float Opacity { get; }
		bool Visible { get; }
		float ParallaxFactorX { get; }
		float ParallaxFactorY { get; }
		Dictionary<string, string> Properties { get; }
	}
}