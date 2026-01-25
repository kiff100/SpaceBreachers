/// ---------------------------------------------
/// Senses Pack for Behavior Designer Pro
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------
namespace Opsive.BehaviorDesigner.AddOns.SensesPack.Runtime.Utility
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEngine;

    /// <summary>
    /// Data structure used to efficiently query objects within a 3D space.
    /// </summary>
    [BurstCompile]
    public struct OctreeNode<T> where T : unmanaged, IPosition, IEquatable<T>
    {
        private Bounds Bounds;
        private NativeList<T> Objects;
        private NativeArray<OctreeNode<T>> Children;

        public Bounds NodeBounds { get => Bounds; }
        public NativeList<T> NodeObjects { get => Objects; }
        public NativeArray<OctreeNode<T>> NodeChildren { get => Children; }

        /// <summary>
        /// Initializes an octree node with a given 3D region and capacity.
        /// </summary>
        /// <param name="bounds">The bounding box for the node.</param>
        /// <param name="capacity">Specifies the maximum number of objects before subdivision.</param>
        public OctreeNode(Bounds bounds, int capacity = 4)
        {
            Bounds = bounds;
            Objects = new NativeList<T>(capacity, Allocator.Persistent);
            Children = default(NativeArray<OctreeNode<T>>);
        }

        /// <summary>
        /// Attempts to insert an object into the octree.
        /// </summary>
        /// <param name="obj">The object that should be inserted.</param>
        /// <returns>The new octree object.</returns>
        [BurstCompile]
        public OctreeNode<T> Insert(T obj)
        {
            return InsertInternal(obj).Item1;
        }

        /// <summary>
        /// Attempts to insert an object into the octree.
        /// </summary>
        /// <param name="obj">The object that should be inserted.</param>
        /// <returns>A tuple containing the new octree object and the status if the object was inserted.</returns>
        [BurstCompile]
        private (OctreeNode<T>, bool) InsertInternal(T obj)
        {
            if (!Bounds.Contains(obj.Position)) {
                return (this, false);
            }

            if (Objects.Length < Objects.Capacity) {
                Objects.Add(obj);
                return (this, true);
            }

            // Subdivide if the the capacity is exceeded.
            if (!Children.IsCreated) {
                Subdivide();
            }

            // Attempt to insert into one of the child nodes.
            for (int i = 0; i < Children.Length; ++i) {
                var insertTuple = Children[i].InsertInternal(obj);
                Children[i] = insertTuple.Item1;
                if (insertTuple.Item2) {
                    return (this, true);
                }
            }

            return (this, false);
        }

        /// <summary>
        /// Retrieves objects within a given search area.
        /// </summary>
        /// <param name="range">The search bounds.</param>
        /// <param name="found">The found objects.</param>
        /// <returns>The number of objects found.</returns>
        [BurstCompile]
        public int Query(Bounds range, NativeList<T> found)
        {
            if (!Bounds.Intersects(range)) {
                return 0;
            }

            var count = 0;
            for (int i = 0; i < Objects.Length; ++i) {
                if (range.Contains(Objects[i].Position)) {
                    found.Add(Objects[i]);
                    count++;
                }
            }

            // The child nodes may also contain objects within the range.
            if (Children.IsCreated) {
                for (int i = 0; i < Children.Length; ++i) {
                    count += Children[i].Query(range, found);
                }
            }
            return count;
        }

        /// <summary>
        /// Subdivides the into eight smaller child nodes.
        /// </summary>
        [BurstCompile]
        private void Subdivide()
        {
            Children = new NativeArray<OctreeNode<T>>(8, Allocator.Persistent);

            var size = Bounds.size / 2f;
            // Generate eight child nodes by shifting the center.
            for (int i = 0; i < 8; i++) {
                var offset = new Vector3(
                    (i & 1) == 0 ? -size.x / 2 : size.x / 2,
                    (i & 2) == 0 ? -size.y / 2 : size.y / 2,
                    (i & 4) == 0 ? -size.z / 2 : size.z / 2
                );

                var childBounds = new Bounds(Bounds.center + offset, size);
                Children[i] = new OctreeNode<T>(childBounds, Objects.Length);
            }
        }

        /// <summary>
        /// Removes an object from the octree.
        /// <param name="obj">The object that should be removed.</param>
        /// </summary>
        /// <returns>The new octree object.</returns>
        public OctreeNode<T> Remove(T obj)
        {
            return RemoveInternal(obj).Item1;
        }

        /// <summary>
        /// Removes an object from the octree.
        /// <param name="obj">The object that should be removed.</param>
        /// </summary>
        /// <returns>A tuple containing the new octree object and the status if the object was removed.</returns>
        [BurstCompile]
        private (OctreeNode<T>, bool) RemoveInternal(T obj)
        {
            if (!Bounds.Contains(obj.Position)) {
                return (this, false);
            }

            for (int i = 0; i < Objects.Length; ++i) {
                if (Objects[i].Equals(obj)) {
                    Objects.RemoveAtSwapBack(i);
                    return (this, true);
                }
            }

            if (Children.IsCreated) {
                for (int i = 0; i < Children.Length; ++i) {
                    var removeTuple = Children[i].RemoveInternal(obj);
                    Children[i] = removeTuple.Item1;
                    if (removeTuple.Item2) {
                        return (this, true);
                    }
                }
            }

            return (this, false);
        }

        /// <summary>
        /// Cleans up any child octree nodes that are no longer used.
        /// </summary>
        [BurstCompile]
        private void Cleanup()
        {
            if (!Children.IsCreated) {
                return;
            }

            // If a child object exists then it should not be disposed.
            for (int i = 0; i < Children.Length; ++i) {
                if (Children[i].Objects.Length > 0) {
                    return;
                }
            }

            // No objects exist anymore - dispose of the object.
            for (int i = 0; i < Children.Length; ++i) {
                Children[i].Dispose();
            }
            Children.Dispose();
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            Objects.Dispose();
            if (Children.IsCreated) {
                for (int i = 0; i < Children.Length; i++) {
                    Children[i].Dispose();
                }
                Children.Dispose();
            }
        }
    }
}