using Nez.Tiled;
using Microsoft.Xna.Framework;
using System.Collections.Generic;


namespace Nez
{
	public class TiledMapRenderer : RenderableComponent, IUpdatable
	{
		public TmxMap TiledMap;

		public int PhysicsLayer = 1 << 0;

		/// <summary>
		/// if null, all layers will be rendered
		/// </summary>
		public int[] LayerIndicesToRender;

		/// <summary>
		/// if null, all layers will be checked for colliders
		/// </summary>
		public int[] LayerIndicesToCollide;

		public bool AutoUpdateTilesets = true;

		public override float Width => TiledMap.Width * TiledMap.TileWidth;
		public override float Height => TiledMap.Height * TiledMap.TileHeight;

		public TmxLayer CollisionLayer;

		bool _shouldCreateColliders;
		private bool _shouldAddTileSetCollisions = false;
		Collider[] _colliders;


		public TiledMapRenderer(TmxMap tiledMap, string collisionLayerName = null, bool shouldCreateColliders = true, bool shouldAddTileSetCollisions = false)
		{
			TiledMap = tiledMap;

			_shouldCreateColliders = shouldCreateColliders;
			_shouldAddTileSetCollisions = shouldAddTileSetCollisions;

			if (collisionLayerName != null)
				CollisionLayer = tiledMap.TileLayers[collisionLayerName];
		}

		/// <summary>
		/// sets this component to only render a single layer
		/// </summary>
		/// <param name="layerName">Layer name.</param>
		public void SetLayerToRender(string layerName)
		{
			LayerIndicesToRender = new int[1];
			LayerIndicesToRender[0] = TiledMap.Layers.IndexOf(TiledMap.GetLayer(layerName));
		}

		/// <summary>
		/// sets which layers should be rendered by this component by name. If you know the indices you can set layerIndicesToRender directly.
		/// </summary>
		/// <param name="layerNames">Layer names.</param>
		public void SetLayersToRender(params string[] layerNames)
		{
			LayerIndicesToRender = new int[layerNames.Length];

			for (var i = 0; i < layerNames.Length; i++)
				LayerIndicesToRender[i] = TiledMap.Layers.IndexOf(TiledMap.GetLayer(layerNames[i]));
		}

		/// <summary>
		/// sets which layers should be checked for collision objects using their layer names.
		/// </summary>
		/// <param name="layerNames">Layer names.</param>
		public void SetLayersToCollide(params string[] layerNames)
		{
			LayerIndicesToCollide = new int[layerNames.Length];

			for (var i = 0; i < layerNames.Length; i++)
				LayerIndicesToCollide[i] = TiledMap.Layers.IndexOf(TiledMap.GetLayer(layerNames[i]));
		}


		#region TiledMap queries

		public int GetRowAtWorldPosition(float yPos)
		{
			yPos -= Entity.Transform.Position.Y + _localOffset.Y;
			return TiledMap.WorldToTilePositionY(yPos);
		}

		public int GetColumnAtWorldPosition(float xPos)
		{
			xPos -= Entity.Transform.Position.X + _localOffset.X;
			return TiledMap.WorldToTilePositionX(xPos);
		}

		/// <summary>
		/// this method requires that you are using a collision layer setup in the constructor.
		/// </summary>
		public TmxLayerTile GetTileAtWorldPosition(Vector2 worldPos)
		{
			Insist.IsNotNull(CollisionLayer, "collisionLayer must not be null!");

			// offset the passed in world position to compensate for the entity position
			worldPos -= Entity.Transform.Position + _localOffset;

			return CollisionLayer.GetTileAtWorldPosition(worldPos);
		}

		/// <summary>
		/// gets all the non-empty tiles that intersect the passed in bounds for the collision layer. The returned List can be put back in the
		/// pool via ListPool.free.
		/// </summary>
		/// <returns>The tiles intersecting bounds.</returns>
		/// <param name="bounds">Bounds.</param>
		public List<TmxLayerTile> GetTilesIntersectingBounds(Rectangle bounds)
		{
			Insist.IsNotNull(CollisionLayer, "collisionLayer must not be null!");

			// offset the passed in world position to compensate for the entity position
			bounds.Location -= (Entity.Transform.Position + _localOffset).ToPoint();
			return CollisionLayer.GetTilesIntersectingBounds(bounds);
		}

		#endregion


		#region Component overrides

		public override void OnEntityTransformChanged(Transform.Component comp)
		{
			// we only deal with positional changes here. TiledMaps cant be scaled.
			if (_shouldCreateColliders && comp == Transform.Component.Position)
			{
				RemoveColliders();
				AddColliders();
			}
		}

		public override void OnAddedToEntity() => AddColliders();

		public override void OnRemovedFromEntity() => RemoveColliders();

		public virtual void Update()
		{
			if (AutoUpdateTilesets)
				TiledMap.Update();
		}

		public override void Render(Batcher batcher, Camera camera)
		{
			if (LayerIndicesToRender == null)
			{
				TiledRendering.RenderMap(TiledMap, batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth, camera.Bounds);
			}
			else
			{
				for (var i = 0; i < TiledMap.Layers.Count; i++)
				{
					if (TiledMap.Layers[i].Visible && LayerIndicesToRender.Contains(i))
						TiledRendering.RenderLayer(TiledMap.Layers[i], batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth, camera.Bounds);
				}
			}
		}

		public override void DebugRender(Batcher batcher)
		{
			foreach (var group in TiledMap.ObjectGroups)
				TiledRendering.RenderObjectGroup(group, batcher, Entity.Transform.Position + _localOffset, Transform.Scale, LayerDepth);

			if (_colliders != null)
			{
				foreach (var collider in _colliders)
					collider.DebugRender(batcher);
			}
		}

		#endregion


		#region Colliders

		public void AddColliders()
		{
			if (!_shouldCreateColliders)
				return; // Adding colliders is disabled

			var colliders = new List<Collider>();

			if (CollisionLayer != null)
			{
				// fetch the collision layer and its rects for collision
				var collisionRects = CollisionLayer.GetCollisionRectangles();

				// create colliders for the rects we received
				for (var i = 0; i < collisionRects.Count; i++)
				{
					var collider = new BoxCollider(collisionRects[i].X + _localOffset.X,
						collisionRects[i].Y + _localOffset.Y, collisionRects[i].Width, collisionRects[i].Height);
					collider.PhysicsLayer = PhysicsLayer;
					collider.Entity = Entity;
					colliders.Add(collider);

					Physics.AddCollider(collider);
				}
			}

			if (_shouldAddTileSetCollisions)
			{
				if (LayerIndicesToCollide == null)
				{
					foreach (var layer in TiledMap.Layers)
					{
						if (layer is TmxLayer tmxLayer && tmxLayer.Visible)
							AddCollidersFromLayer(tmxLayer, colliders);
						// todo handle other layer types?
						// else if (layer is TmxImageLayer tmxImageLayer && tmxImageLayer.Visible)
						// 	RenderImageLayer(tmxImageLayer, batcher, position, scale, layerDepth);
						// else if (layer is TmxGroup tmxGroup && tmxGroup.Visible)
						// 	RenderGroup(tmxGroup, batcher, position, scale, layerDepth);
						// else if (layer is TmxObjectGroup tmxObjGroup && tmxObjGroup.Visible)
						// 	RenderObjectGroup(tmxObjGroup, batcher, position, scale, layerDepth);
					}
				}
				else
				{
					foreach (int layer in LayerIndicesToCollide)
					{
						AddCollidersFromLayer(TiledMap.TileLayers[layer], colliders);
					}
				}
			}

			_colliders = colliders.ToArray();
		}

		private void AddCollidersFromLayer(TmxLayer layer, List<Collider> listToAppend)
		{
			foreach (var tile in layer.Tiles)
			{
				if (tile == null)
					continue;

				var objects = tile.TilesetTile?.ObjectGroups[0]?.Objects;
				if (objects != null)
				{
					foreach (var obj in objects)
					{
						if (obj.ObjectType is TmxObjectType.Ellipse)
						{
							// Add ellipse collider
							var collider = new CircleCollider();
							var objOffset = new Vector2(obj.X, obj.Y);
							
							// tiled does not support adding ellipses, so no need to scale the circle.
							collider.Radius = TiledMap.TileWidth / 4.0f;

							collider.Entity = Entity;
							collider.LocalOffset += new Vector2(tile.X * TiledMap.TileWidth, tile.Y * TiledMap.TileHeight) + _localOffset + objOffset;
							collider.PhysicsLayer = PhysicsLayer;
							listToAppend.Add(collider);

							Physics.AddCollider(collider);
						}
						else if (obj.ObjectType is TmxObjectType.Polygon)
						{
							var collider = new PolygonCollider(obj.Points);
							var objOffset = new Vector2(obj.X, obj.Y);
							collider.Entity = Entity;
							collider.LocalOffset += new Vector2(tile.X * TiledMap.TileWidth, tile.Y * TiledMap.TileHeight) + _localOffset + objOffset;
							collider.PhysicsLayer = PhysicsLayer;
							listToAppend.Add(collider);

							Physics.AddCollider(collider);
						}
						// var collider = new BoxCollider(tile.X * TiledMap.TileWidth, tile.Y * TiledMap.TileHeight, TiledMap.TileWidth, TiledMap.TileHeight);
						// var objOffset = new Vector2(obj.X, obj.Y);
						// collider.Entity = Entity;
						// collider.LocalOffset += _localOffset;
						// collider.PhysicsLayer = PhysicsLayer;
						// listToAppend.Add(collider);

						
					}
				}
			}
		}

		public void RemoveColliders()
		{
			if (_colliders == null)
				return;

			foreach (var collider in _colliders)
				Physics.RemoveCollider(collider);
			_colliders = null;
		}

		#endregion
	}
}
