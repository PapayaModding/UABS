using System;

namespace UABS.Assets.Script.__Test__.TestUtil
{
    public interface ITestable
    {
        void Test(Action onComplete);
    }
}