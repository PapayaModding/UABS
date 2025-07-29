using System.Runtime.InteropServices;
using UnityEngine;

public class TestNative : MonoBehaviour
{
    [DllImport("minimal", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint TestFunc(int a);

    void Start()
    {
        Debug.Log("Add(2,3) = " + TestFunc(2));
    }
}