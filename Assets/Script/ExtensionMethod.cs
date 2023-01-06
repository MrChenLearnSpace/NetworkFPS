using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethod 
{
    // Start is called before the first frame update
    public static Transform DFSFindChildGameObject(this Transform scr, string targetName) {
        if (scr.childCount <= 0)
            return null;
        for (int i = 0; i < scr.childCount; i++) {
            if (scr.GetChild(i).name == targetName)
                return scr.GetChild(i);
            // print(scr.GetChild(i));
            Transform temp = DFSFindChildGameObject(scr.GetChild(i), targetName);
            if (temp)
                return temp;

        }
        return null;
    }
}
