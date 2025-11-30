using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Text
{
    internal class DynamicMesh : Mesh
    {

        public new DynamicVertexBuffer VertexBuffer;
        public new DynamicIndexBuffer IndexBuffer;

        public DynamicMesh(GraphicsDevice GraphicsDevice, Vector3 position, Vector3 origin, float depth, uint[] index, VertexPosition[] vertex) : base(GraphicsDevice, position, origin, depth, index, vertex)
        {
        }


        public override void Buffer(GraphicsDevice GraphicsDevice)
        {
            if (Empty()) return;

            this.IndexBuffer = new(GraphicsDevice, IndexElementSize.ThirtyTwoBits, index.Length, BufferUsage.WriteOnly);
            this.IndexBuffer.SetData(index);

            this.VertexBuffer = new(GraphicsDevice, typeof(VertexPosition), vertex.Length, BufferUsage.WriteOnly);
            this.VertexBuffer.SetData(vertex);
        }

        public override void Draw(GraphicsDevice GraphicsDevice, Camera Camera, Effect Effect)
        {

            if (Empty()) return;

            Effect.Parameters["View"].SetValue(Matrix.CreateTranslation(Camera.position));
            Effect.Parameters["World"].SetValue(Matrix.CreateScale(1, 1, 1) * Matrix.CreateRotationZ(0) * Matrix.CreateTranslation(new Vector3(position.X + origin.X, position.Y + origin.Y, depth)));
            Effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(0.800f * Camera.zoom, 0.480f * Camera.zoom, 0, -1f));

            Effect.Parameters["VertexColorEnabled"].SetValue(1);
            Effect.Parameters["TextureColorEnabled"].SetValue(0);

            GraphicsDevice.Indices = IndexBuffer;
            GraphicsDevice.SetVertexBuffer(VertexBuffer);

            foreach (EffectPass Pass in Effect.CurrentTechnique.Passes)
            {

                Pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexBuffer.IndexCount / 3);

            }
        }
    }
}
