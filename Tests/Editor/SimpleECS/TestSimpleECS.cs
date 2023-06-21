using System.Linq;
using NUnit.Framework;
using Puffercat.Uxt.SimpleECS;
using UnityEngine;

namespace Puffercat.Uxt.Tests.Editor.SimpleECS
{
    public class TestSimpleECS
    {
        private EntityRegistry m_registry;

        private struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        private record EnemyTag : TagComponent<EnemyTag>;

        private record FriendlyTag : TagComponent<FriendlyTag>;

        private record NeutralTag : TagComponent<NeutralTag>;

        private class TransformComponent : WrappedStruct<TransformData>
        {
            public TransformComponent()
            {
                Data = new TransformData
                {
                    position = Vector3.zero,
                    rotation = Quaternion.identity,
                    scale = Vector3.one
                };
            }
        }

        [SetUp]
        public void SetUp()
        {
            m_registry = new EntityRegistry();
        }

        [Test]
        public void SimpleIteration()
        {
            m_registry
                .CreateEntity()
                .AddComponent<EnemyTag>()
                .AddComponent<TransformComponent>();

            m_registry
                .CreateEntity()
                .AddComponent<FriendlyTag>()
                .AddComponent<TransformComponent>();

            Assert.AreEqual(2, m_registry.IterateEntities<TransformComponent>().Count());
            Assert.AreEqual(1, m_registry.IterateEntities<EnemyTag>().Count());
            Assert.AreEqual(1, m_registry.IterateEntities<FriendlyTag>().Count());
            Assert.AreEqual(1, m_registry.IterateEntities<TransformComponent, FriendlyTag>().Count());
            Assert.AreEqual(0, m_registry.IterateEntities<NeutralTag>().Count());
        }

        [Test]
        public void AddingComponentDoesNotProlongIteration()
        {
            m_registry.CreateEntity().AddComponent<NeutralTag>();
            Assert.AreEqual(1, m_registry.IterateEntities<NeutralTag>().Count());

            void Duplicate()
            {
                var oldCount = m_registry.IterateEntities<NeutralTag>().Count();
                var iterCount = 0;
                foreach (var _ in m_registry.IterateEntities<NeutralTag>())
                {
                    m_registry.CreateEntity().AddComponent<NeutralTag>();
                    ++iterCount;
                }
                Assert.AreEqual(oldCount, iterCount);
            }
            
            Duplicate();
            Duplicate();
            Duplicate();
            
            Assert.AreEqual(8, m_registry.IterateEntities<NeutralTag>().Count());
        }

        [Test]
        public void ComponentRemovalIsDelayed()
        {
            for (var i = 0; i != 8; ++i)
            {
                m_registry.CreateEntity().AddComponent<NeutralTag>();
            }

            void RemoveHalf()
            {
                var removeFlag = false;
                var oldCount = m_registry.IterateEntities<NeutralTag>().Count();
                var iterCount = 0;
                foreach (var (entity, _) in m_registry.IterateEntities<NeutralTag>())
                {
                    if (removeFlag)
                    {
                        m_registry.DestroyEntity(entity.GetHandle());
                    }

                    ++iterCount;
                    removeFlag = !removeFlag;
                }
                
                Assert.AreEqual(oldCount, iterCount);
                m_registry.PerformPendingDestruction();
                Assert.AreEqual(oldCount / 2, m_registry.IterateEntities<NeutralTag>().Count());
            }
            
            RemoveHalf();
            RemoveHalf();
            RemoveHalf();
            
            Assert.AreEqual(1, m_registry.IterateEntities<NeutralTag>().Count());
        }
        
        [Test]
        public void ComponentRemovalIsDelayed_NoHandle()
        {
            for (var i = 0; i != 8; ++i)
            {
                m_registry.CreateEntity().AddComponent<NeutralTag>();
            }

            void RemoveHalf()
            {
                var removeFlag = false;
                var oldCount = m_registry.IterateEntities<NeutralTag>().Count();
                var iterCount = 0;
                foreach (var (entity, _) in m_registry.IterateEntities<NeutralTag>())
                {
                    if (removeFlag)
                    {
                        entity.Destroy();
                    }

                    ++iterCount;
                    removeFlag = !removeFlag;
                }
                
                Assert.AreEqual(oldCount, iterCount);
                m_registry.PerformPendingDestruction();
                Assert.AreEqual(oldCount / 2, m_registry.IterateEntities<NeutralTag>().Count());
            }
            
            RemoveHalf();
            RemoveHalf();
            RemoveHalf();
            
            Assert.AreEqual(1, m_registry.IterateEntities<NeutralTag>().Count());
        }
    }
}