using System.Collections.Generic;
using UnityEngine;

namespace HandPosing
{
    public static class HandBoneSearcher
    {
        public static class BoneSearcher
        {
            public struct BoneNaming
            {
                public string finger;
                public int jointIndex;

                public BoneNaming(string finger, int joint)
                {
                    this.finger = finger;
                    this.jointIndex = joint;
                }
            }

            public static BoneNaming? GetName(BoneId bone)
            {
                if(BoneNamings.TryGetValue(bone, out BoneNaming name))
                {
                    return name;
                }
                return null;
            }

            public static List<BoneMap> FindBones(Transform parent)
            {
                List<BoneMap> bones = new List<BoneMap>();

                foreach(var name in BoneNamings.Keys)
                {
                    


                }

                return bones;

            }

            private static readonly Dictionary<BoneId, BoneNaming> BoneNamings = new Dictionary<BoneId, BoneNaming>()
            {
                {BoneId.Invalid, new BoneNaming("none",-1) },
                {BoneId.Hand_Start, new BoneNaming("start",-1) },

                {BoneId.Hand_Thumb0, new BoneNaming("thumb",0) },
                {BoneId.Hand_Thumb1, new BoneNaming("thumb",1) },
                {BoneId.Hand_Thumb2, new BoneNaming("thumb",2) },
                {BoneId.Hand_Thumb3, new BoneNaming("thumb",3) },

                {BoneId.Hand_Index1, new BoneNaming("index",1) },
                {BoneId.Hand_Index2, new BoneNaming("index",2) },
                {BoneId.Hand_Index3, new BoneNaming("index",3) },

                {BoneId.Hand_Middle1, new BoneNaming("middle",1) },
                {BoneId.Hand_Middle2, new BoneNaming("middle",2) },
                {BoneId.Hand_Middle3, new BoneNaming("middle",3) },

                {BoneId.Hand_Ring1, new BoneNaming("ring",1) },
                {BoneId.Hand_Ring2, new BoneNaming("ring",2) },
                {BoneId.Hand_Ring3, new BoneNaming("ring",3) },

                {BoneId.Hand_Pinky0, new BoneNaming("pinky",0) },
                {BoneId.Hand_Pinky1, new BoneNaming("pinky",1) },
                {BoneId.Hand_Pinky2, new BoneNaming("pinky",2) },
                {BoneId.Hand_Pinky3, new BoneNaming("pinky",3) },
            };
        }
    }
}
