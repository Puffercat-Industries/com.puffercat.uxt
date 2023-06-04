using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Puffercat.Uxt.Containers;
using UnityEngine;
using Random = System.Random;

namespace Puffercat.Uxt.Tests.Editor.Containers
{
    public class TestSafeIterationCollection
    {
        private SafeIterationCollection<string> m_collection;

        [SetUp]
        public void SetUp()
        {
            m_collection = new SafeIterationCollection<string>()
            {
                "apple",
                "orange",
                "pear",
                "cat",
                "dog"
            };
        }

        [Test]
        public void EmptyCollection()
        {
            var empty = new SafeIterationCollection<string>();
            Assert.IsEmpty(empty);
        }

        [Test]
        public void CollectionInitializedCorrectly()
        {
            var set = m_collection.ToHashSet();
            Assert.True(set.SetEquals(new[] { "apple", "orange", "pear", "cat", "dog" }));
            Assert.AreEqual(5, m_collection.Count);
        }

        [Test]
        public void MultipleEnumerationThrows()
        {
            foreach (var _ in m_collection)
            {
                Assert.Throws<MultipleEnumerationException>(() => m_collection.GetEnumerator());
            }
        }

        [Test]
        public void AddAndRemove()
        {
            var handle = m_collection.Add("added");
            Assert.AreEqual(handle.Value, "added");
            Assert.True(m_collection.ToHashSet().SetEquals(new[] { "apple", "orange", "pear", "cat", "dog", "added" }));
            Assert.AreEqual(6, m_collection.Count);
        }

        [Test]
        public void Random_4_4()
        {
            RandomAddRemoveDuringIteration(4, 4);
        }

        [Test]
        public void Random_100_100()
        {
            RandomAddRemoveDuringIteration(100, 100);
        }

        [Test]
        public void Random_500_50()
        {
            RandomAddRemoveDuringIteration(500, 10);
        }

        private void RandomAddRemoveDuringIteration(int iterationCount, int maxAddPerIteration)
        {
            var elements = new List<int>();
            var handles = new List<SafeIterationCollection<int>.Handle>();
            var counter = 0;

            var rng = new Random(42);
            var collection = new SafeIterationCollection<int>();

            var totalAddElementCount = maxAddPerIteration * iterationCount;

            void Instance(HashSet<int> removedElements, int addCount, bool noRemoval = false)
            {
                addCount = Mathf.Min(totalAddElementCount, addCount);
                totalAddElementCount -= addCount;
                
                var removeCount = rng.Next(elements.Count + addCount);
                
                if (noRemoval)
                {
                    removeCount = 0;
                }

                while (addCount > 0 || removeCount > 0) 
                {
                    Assert.AreEqual(elements.Count, collection.Count);

                    // 0 is remove, 1 is add; if is empty, then we can only add
                    var action = rng.Next(2);
                    if (elements.Count == 0 || noRemoval || removeCount == 0)
                    {
                        action = 1;
                    }

                    if (addCount == 0)
                    {
                        action = 0;
                    }

                    if (action == 0)
                    {
                        // Do a removal
                        var handleIndex = rng.Next(handles.Count);
                        var handle = handles[handleIndex];

                        Assert.True(handle.IsValid());
                        Assert.AreEqual(elements[handleIndex], handle.Value);

                        removedElements.Add(handle.Value);
                        handles.RemoveAt(handleIndex);
                        elements.RemoveAt(handleIndex);
                        handle.RemoveFromCollection();

                        Assert.False(handle.IsValid());
                        Assert.AreEqual(elements.Count, collection.Count);

                        --removeCount;
                    }
                    else if (action == 1)
                    {
                        // Do an add
                        var elem = counter++;
                        elements.Add(elem);
                        var handle = collection.Add(elem);
                        handles.Add(handle);
                        Assert.AreEqual(elements.Count, collection.Count);

                        --addCount;
                    }
                }
            }

            Instance(null, 10, true);
            
            for (var i = 0; i < iterationCount; ++i)
            {
                var iteratedElements = new HashSet<int>();
                var removedElements = new HashSet<int>();
                
                foreach (var val in collection)
                {
                    iteratedElements.Add(val);
                    
                    Instance(removedElements, rng.Next(maxAddPerIteration));
                }
                
                // Iterated elements are in the current set or those removed during iteration
                Assert.True(iteratedElements.IsSubsetOf(elements.ToHashSet().Union(removedElements)));

                // All elements (original and added) that were not removed were iterated
                Assert.True(elements.ToHashSet().IsSubsetOf(iteratedElements));
            }
        }
    }
}