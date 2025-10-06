using UnityEngine;

class SmoothForce
{
    private Vector3[] accForce = new Vector3[2];
    private int curIndex = 0;

    public SmoothForce()
    {
        clear();
    }

    public void clear()
    {
        for (int i = 0; i < accForce.Length; ++i)
            accForce[i] = Vector3.zero;
        curIndex = 0;
    }

    public Vector3 get(Vector3 force)
    {
        accForce[curIndex++] = force;
        if (curIndex >= accForce.Length)
            curIndex = 0;

        Vector3 ret = Vector3.zero;
        for (int i = 0; i < accForce.Length; ++i)
            ret += accForce[i];
        return ret / accForce.Length;
    }
}
