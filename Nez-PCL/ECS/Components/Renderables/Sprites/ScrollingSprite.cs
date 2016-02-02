﻿using System;
using Nez.Sprites;
using Nez.Textures;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;


namespace Nez.Sprites
{
	public class ScrollingSprite : Sprite, IUpdatable
	{
		/// <summary>
		/// x speed of automatic scrolling
		/// </summary>
		public float scrollSpeedX = 0;

		/// <summary>
		/// y speed of automatic scrolling
		/// </summary>
		public float scrollSpeedY = 0;

		/// <summary>
		/// x value of the texture scroll
		/// </summary>
		/// <value>The scroll x.</value>
		public int scrollX
		{
			get { return _sourceRect.X; }
			set { _sourceRect.X = value; }
		}

		/// <summary>
		/// y value of the texture scroll
		/// </summary>
		/// <value>The scroll y.</value>
		public int scrollY
		{
			get { return _sourceRect.Y; }
			set { _sourceRect.Y = value; }
		}

		/// <summary>
		/// we keep a copy of the sourceRect so that we dont change the Subtexture in case it is used elsewhere
		/// </summary>
		Rectangle _sourceRect;


		public ScrollingSprite( Subtexture subtexture ) : base( subtexture )
		{
			renderState = new RenderState();
			renderState.samplerState = SamplerState.PointWrap;
			_sourceRect = subtexture.sourceRect;
		}


		public ScrollingSprite( Texture2D texture ) : this( new Subtexture( texture ) )
		{}


		public void update()
		{
			_sourceRect.X += (int)( scrollSpeedX * Time.deltaTime );
			_sourceRect.Y += (int)( scrollSpeedY * Time.deltaTime );
		}


		public override void render( Graphics graphics, Camera camera )
		{
			if( isVisibleFromCamera( camera ) )
				graphics.spriteBatch.Draw( subtexture, entity.transform.position + _localPosition, _sourceRect, color, entity.transform.rotation, origin, entity.transform.scale, spriteEffects, _layerDepth );
		}

	}
}

