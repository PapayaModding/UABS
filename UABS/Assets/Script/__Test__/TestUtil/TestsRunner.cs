using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UABS.Assets.Script.__Test__.TestUtil
{
    public class TestsRunner : MonoBehaviour
    {
        private List<ITestable> testables = new();

        private void Start()
        {
            var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var behaviour in allBehaviours)
            {
                if (behaviour is ITestable testable)
                {
                    if (IsTrulyEnabled(behaviour))
                        testables.Add(testable);
                }
            }

            StartCoroutine(RunAllTests());
        }

        private bool IsTrulyEnabled(MonoBehaviour mono)
        {
            return mono != null && mono.enabled && mono.gameObject.activeInHierarchy;
        }

        private IEnumerator RunAllTests()
        {
            foreach (ITestable testable in testables)
            {
                bool isDone = false;
                testable.Test(() =>
                {
                    isDone = true;
                    // Debug.Log($"{testable} is done");
                });
                yield return new WaitUntil(() => isDone);
            }

            Debug.Log("All tests completed.");
        }
    }
}