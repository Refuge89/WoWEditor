﻿using System;
using SharpDX;
using WoWEditor6.Graphics;
using WoWEditor6.Scene.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WoWEditor6.Scene.Terrain
{
    class MapAreaRender : IDisposable
    {
        private bool mAsyncLoaded;
        private bool mSyncLoaded;

        private VertexBuffer mVertexBuffer;
        private readonly MapChunkRender[] mChunks = new MapChunkRender[256];
        private BoundingBox mBoundingBox;
        private BoundingBox mModelBox;
        private bool mIsDirty;

        public int IndexX { get; private set; }
        public int IndexY { get; private set; }

        public IO.Files.Terrain.MapArea AreaFile { get; private set; }

        public MapAreaRender(int indexX, int indexY)
        {
            IndexX = indexX;
            IndexY = indexY;
        }

        public void OnTextureChange(Editing.TextureChangeParameters parameters)
        {
            if (mAsyncLoaded == false || AreaFile.IsValid == false || mSyncLoaded == false)
                return;

            AreaFile.OnTextureTerrain(parameters);
        }

        public void OnTerrainChange(Editing.TerrainChangeParameters parameters)
        {
            if (mAsyncLoaded == false || AreaFile.IsValid == false || mSyncLoaded == false)
                return;

            mIsDirty = AreaFile.OnChangeTerrain(parameters);
            if (!mIsDirty)
                return;

            mBoundingBox = AreaFile.BoundingBox;
            foreach (var chunk in mChunks)
            {
                if (chunk == null) continue;

                chunk.UpdateBoundingBox();
            }
        }

        public void OnUpdateModelPositions(Editing.TerrainChangeParameters parameters)
        {
            if (mAsyncLoaded == false || AreaFile.IsValid == false || mSyncLoaded == false)
                return;

            AreaFile.OnUpdateModelPositions(parameters);
        }

        public void OnFrame()
        {
            if (mAsyncLoaded == false)
                return;

            if (AreaFile.IsValid == false)
                return;

            if(mSyncLoaded == false)
            {
                mVertexBuffer = new VertexBuffer(WorldFrame.Instance.GraphicsContext);
                mVertexBuffer.UpdateData(AreaFile.FullVertices);
                mSyncLoaded = true;
            }

            if(WorldFrame.Instance.MapManager.IsInitialLoad == false)
            {
                if (WorldFrame.Instance.ActiveCamera.Contains(ref mBoundingBox) == false)
                {
                    if (!M2Manager.IsViewDirty)
                        return;

                    if (!WorldFrame.Instance.ActiveCamera.Contains(ref mModelBox))
                        return;

                    foreach (var chunk in mChunks)
                        chunk.PushDoodadReferences();

                    return;
                }
            }

            if (mIsDirty)
            {
                AreaFile.UpdateNormals();
                mVertexBuffer.UpdateData(AreaFile.FullVertices);
                mIsDirty = false;
            }

            MapChunkRender.ChunkMesh.UpdateVertexBuffer(mVertexBuffer);

            foreach (var chunk in mChunks)
                chunk.OnFrame();
        }

        public void AsyncLoaded(IO.Files.Terrain.MapArea area)
        {
            AreaFile = area;
            if (AreaFile.IsValid == false)
                return;

            for(var i = 0; i < 256; ++i)
            {
                var chunk = new MapChunkRender();
                chunk.OnAsyncLoad(area.GetChunk(i), this);
                mChunks[i] = chunk;
            }

            mBoundingBox = area.BoundingBox;
            mModelBox = area.ModelBox;
            mAsyncLoaded = true;
        }

        public void Dispose()
        {
            if(AreaFile != null)
                AreaFile.Dispose();
            AreaFile = null;

            mAsyncLoaded = false;
            var vertexBuffer = mVertexBuffer;
            mVertexBuffer = null;
            WorldFrame.Instance.Dispatcher.BeginInvoke(() => { if (vertexBuffer != null) vertexBuffer.Dispose(); });

            for(var i = 0; i < 256; ++i)
            {
                if (mChunks[i] == null) continue;

                mChunks[i].Dispose();
                mChunks[i] = null;
            }
        }
    }
}
