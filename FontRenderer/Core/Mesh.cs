using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Text
{
    internal class Mesh
    {
        public Vector3 position;
        public Vector3 origin;
        public float depth;

        public uint[] index;
        public VertexPosition[] vertex;
        public Vector2[] physics;

        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;

        public DynamicVertexBuffer DynamicVertexBuffer;
        public DynamicIndexBuffer DynamicIndexBuffer;


        public Mesh(GraphicsDevice GraphicsDevice, Vector3 position, Vector3 origin, float depth, uint[] index, VertexPosition[] vertex)
        {
            this.origin = origin;
            this.position = position;
            this.depth = depth;
            this.index = index;
            this.vertex = vertex;

            this.Buffer(GraphicsDevice);
        }

        public virtual void Buffer(GraphicsDevice GraphicsDevice)
        {
            if (Empty())
                return;

            IndexBuffer = new(GraphicsDevice, IndexElementSize.ThirtyTwoBits, index.Length, BufferUsage.WriteOnly);
            IndexBuffer.SetData(index);

            VertexBuffer = new(GraphicsDevice, typeof(VertexPosition), vertex.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertex);
        }

        public virtual bool Empty()
        {
            return (vertex?.Length ?? 0) <= 3 || (index?.Length ?? 0) <= 3;

        }


        public virtual void Draw(GraphicsDevice GraphicsDevice, Camera Camera, Effect Effect)
        {
            if (Empty()) return;

            Effect.Parameters["View"].SetValue(Matrix.CreateTranslation(Camera.position));
            Effect.Parameters["World"].SetValue(Matrix.CreateScale(1, 1, 1) * Matrix.CreateRotationZ(0) * Matrix.CreateTranslation(new Vector3(position.X + origin.X, position.Y + origin.Y, depth)));
            Effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(0.800f * Camera.zoom, 0.480f * Camera.zoom, 0, -1f));

            Effect.Parameters["VertexColorEnabled"].SetValue(1);
            Effect.Parameters["TextureColorEnabled"].SetValue(0);

            Effect.Parameters["Color"].SetValue(Color.Black.ToVector4());

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
