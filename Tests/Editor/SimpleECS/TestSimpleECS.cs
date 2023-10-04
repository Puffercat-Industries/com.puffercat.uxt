using System.Linq;
using NUnit.Framework;
using Puffercat.Uxt.ECS.Core;
using UnityEngine;

namespace Puffercat.Uxt.Tests.Editor.SimpleECS
{
    public class TestSimpleEcs
    {
        private EntityRegistry m_registry;

        private struct TransformComponent : IEntityComponent<TransformComponent>
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        private struct EnemyTag : IEntityComponent<EnemyTag>
        {
        }

        private struct FriendlyTag : IEntityComponent<FriendlyTag>
        {
        }

        private struct NeutralTag : IEntityComponent<NeutralTag>
        {
        }

        [SetUp]
        public void SetUp()
        {
            m_registry = new EntityRegistry();
        }

        [Test]
        public void SimpleIteration()
        {
            {
                var entity = m_registry.CreateEntity();
                m_registry.AddOrGetComponent<EnemyTag>(entity);
                m_registry.AddOrGetComponent<TransformComponent>(entity);
            }

            {
                var entity = m_registry.CreateEntity();
                m_registry.AddOrGetComponent<FriendlyTag>(entity);
                m_registry.AddOrGetComponent<TransformComponent>(entity);
            }

            Assert.AreEqual(2, m_registry.GetAllEntitiesWithComponent<TransformComponent>().Count());
            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<EnemyTag>().Count());
            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<FriendlyTag>().Count());
            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<TransformComponent, FriendlyTag>().Count());
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count());
        }

        [Test]
        public void AddingComponentDoesNotProlongIteration()
        {
            {
                var entity = m_registry.CreateEntity();
                m_registry.AddOrGetComponent<NeutralTag>(entity);
            }

            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count());

            void Duplicate()
            {
                var oldCount = m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count();
                var iterCount = 0;
                foreach (var _ in m_registry.GetAllEntitiesWithComponent<NeutralTag>())
                {
                    var entity = m_registry.CreateEntity();
                    m_registry.AddOrGetComponent<NeutralTag>(entity);
                    ++iterCount;
                }

                Assert.AreEqual(oldCount, iterCount);
            }

            Duplicate();
            Duplicate();
            Duplicate();

            Assert.AreEqual(8, m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count());
        }


        [Test]
        public void ComponentRemovalIsDelayed()
        {
            for (var i = 0; i != 8; ++i)
            {
                var entity = m_registry.CreateEntity();
                m_registry.AddOrGetComponent<NeutralTag>(entity);
            }

            void RemoveHalf()
            {
                var removeFlag = false;
                var oldCount = m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count();
                var iterCount = 0;
                foreach (var entity in m_registry.GetAllEntitiesWithComponent<NeutralTag>())
                {
                    if (removeFlag)
                    {
                        m_registry.MarkEntityForDestruction(entity);
                    }

                    ++iterCount;
                    removeFlag = !removeFlag;
                }

                Assert.AreEqual(oldCount, iterCount);
                m_registry.ProcessDestruction();
                Assert.AreEqual(oldCount / 2,
                    m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count()); // Updated method name
            }

            RemoveHalf();
            RemoveHalf();
            RemoveHalf();

            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count()); // Updated method name
        }

        [Test]
        public void SimpleAddAndRemoveComponent()
        {
            var callbackInvocationCounter = 0;

            for (var i = 0; i < 512; ++i)
            {
                var entity = m_registry.CreateEntity();
                ref var transform = ref m_registry.AddOrGetComponent<TransformComponent>(entity);

                if (i % 2 == 1)
                {
                    m_registry.AddOrGetComponent<FriendlyTag>(entity);
                    m_registry.AddComponentDestructionCallback<FriendlyTag>(entity, _ =>
                    {
                        Assert.True(m_registry.HasComponent(entity, ComponentTypeId<FriendlyTag>.Value));
                        callbackInvocationCounter--;
                    });
                    transform.position = new Vector3(i, i, i);
                }
                else
                {
                    m_registry.AddOrGetComponent<EnemyTag>(entity);
                    m_registry.AddComponentDestructionCallback<EnemyTag>(entity, _ =>
                    {
                        Assert.True(m_registry.HasComponent(entity, ComponentTypeId<EnemyTag>.Value));
                        callbackInvocationCounter++;
                    });
                }
            }

            Assert.AreEqual(512, m_registry.GetAllEntitiesWithComponent<TransformComponent>().Count());
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<NeutralTag>().Count());
            Assert.AreEqual(256, m_registry.GetAllEntitiesWithComponent<FriendlyTag>().Count());
            Assert.AreEqual(256, m_registry.GetAllEntitiesWithComponent<EnemyTag>().Count());

            foreach (var entity in m_registry.GetAllEntitiesWithComponent<TransformComponent, FriendlyTag>())
            {
                Assert.True(m_registry.HasComponent(entity, ComponentTypeId<TransformComponent>.Value));
                Assert.True(m_registry.HasComponent(entity, ComponentTypeId<FriendlyTag>.Value));

                ref var transform = ref m_registry.TryGetComponent<TransformComponent>(entity).Value;

                Assert.Greater(transform.position.x, 0.0f);
                Assert.True(Mathf.Approximately(transform.position.x, transform.position.y));
                Assert.True(Mathf.Approximately(transform.position.y, transform.position.z));
            }

            foreach (var entity in m_registry.GetAllEntitiesWithComponent<TransformComponent>())
            {
                if (m_registry.HasComponent(entity, ComponentTypeId<EnemyTag>.Value))
                {
                    m_registry.MarkComponentForRemoval<EnemyTag>(entity);
                    var failedToRemoveFriendlyTag = m_registry.MarkComponentForRemoval<FriendlyTag>(entity);
                    Assert.False(failedToRemoveFriendlyTag);
                }
            }

            m_registry.ProcessDestruction();

            Assert.AreEqual(256, callbackInvocationCounter);
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<EnemyTag>().Count());

            foreach (var entity in m_registry.GetAllEntitiesWithComponent<FriendlyTag>())
            {
                m_registry.MarkEntityForDestruction(entity);
            }

            foreach (var entity in m_registry.GetAllEntitiesWithComponent<TransformComponent>())
            {
                m_registry.MarkComponentForRemoval<TransformComponent>(entity);
            }

            m_registry.ProcessDestruction();

            Assert.AreEqual(0, callbackInvocationCounter);
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<FriendlyTag>().Count());
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<TransformComponent>().Count());
            Assert.AreEqual(0, m_registry.GetAllEntitiesWithComponent<EnemyTag>().Count());
        }

        public struct CopyMarker : IEntityComponent<CopyMarker>
        {
        }

        [Test]
        public void CopyingComponent()
        {
            var original = m_registry.CreateEntity();
            m_registry.AddOrGetComponent<TransformComponent>(original).position = Vector3.one;
            var copied = m_registry.CopyEntity(original);
            m_registry.AddOrGetComponent<CopyMarker>(copied);
            m_registry.TryGetComponent<TransformComponent>(copied).Value.position *= 2;

            Assert.AreEqual(2, m_registry.GetAllEntitiesWithComponent<TransformComponent>().Count());
            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<CopyMarker>().Count());
            Assert.AreEqual(1, m_registry.GetAllEntitiesWithComponent<CopyMarker, TransformComponent>().Count());
            Assert.AreEqual(copied, m_registry.GetAllEntitiesWithComponent<CopyMarker>().First());
            Assert.True(
                Vector3.Distance(Vector3.one, m_registry.TryGetComponent<TransformComponent>(original).Value.position) <
                0.01f);
        }

        [Test]
        public void TestCallbackRemoval()
        {
            var callbackCounter = 0;
            var entity = m_registry.CreateEntity();
            m_registry.AddOrGetComponent<TransformComponent>(entity);

            void Callback(Entity inEntity)
            {
                Assert.AreEqual(entity, inEntity);
                ++callbackCounter;
            }
            
            var nullCallbackHandle = m_registry.AddComponentDestructionCallback<NeutralTag>(entity, Callback);
            Assert.AreEqual(default(ComponentDestructionCallbackHandle), nullCallbackHandle);
            var transformDestructionCallback =
                m_registry.AddComponentDestructionCallback<TransformComponent>(entity, Callback);
            m_registry.MarkEntityForDestruction(entity);
            m_registry.RemoveComponentDestructionCallback(transformDestructionCallback);
            m_registry.ProcessDestruction();
            Assert.AreEqual(0, callbackCounter);
        }
    }
}