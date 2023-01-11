using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Nez.AI.BehaviorTrees;
using Nez.Tiled;

namespace Nez.ECS.Components.Renderables
{
	/// <summary>
	/// For any tiles that are animated, they should update so long as the associated map is updated.
	/// </summary>
	public class TiledTileComponent : RenderableComponent, IUpdatable
	{
		public override float Width => Layer.Map.TileWidth;
		public override float Height => Layer.Map.TileHeight;

		private readonly Camera camera;
		public ITmxLayer Layer { get; }
		public TmxLayerTile Tile { get; }
		public bool LayerDepthFromYCoordinate { get; }

		public TiledTileComponent(ITmxLayer layer, TmxLayerTile tile, Camera camera, bool layerDepthFromYCoordinate = true)
		{
			this.camera = camera;
			Layer = layer;
			Tile = tile;
			LayerDepthFromYCoordinate = layerDepthFromYCoordinate;

			// Set local offset for correct IsVisible calculations
			LocalOffset = new Vector2(Tile.X * Width, Tile.Y * Height);
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			var color = Color.White;
			color.A = (byte)(Layer.Opacity * 255);

			TiledRendering.RenderTile(Tile, batcher, Transform.Position,
				Transform.Scale, Width, Height,
				color, LayerDepth, Layer.Map.Orientation,
				Layer.Map.Width, Layer.Map.Height);
			// TiledRendering.RenderTile(Layer, batcher, Transform.Position + _localOffset, Transform.Scale, LayerDepth, camera.Bounds);
		}

		public void Update()
		{
			var tileHeight = Layer.Map.TileHeight * Transform.Scale.Y;
			
			// update the render depth:
			if (LayerDepthFromYCoordinate)
			{
				var tileYPosWorld = Transform.Position.Y + _localOffset.Y + tileHeight / 2.0f; // centered on tile
				var tileYCameraSpace = (tileYPosWorld - camera.Bounds.Top);

				if (IsVisible)
				{
					LayerDepth = Mathf.Clamp01(1 - tileYCameraSpace / camera.Bounds.Height);
					Debug.LogIf(Tile.X == 50 && Tile.Y == 54, $"LayerDepth {LayerDepth}");
				}
			}
		}
	}
}
