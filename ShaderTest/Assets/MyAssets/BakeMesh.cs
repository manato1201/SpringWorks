using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakeMesh : MonoBehaviour
{
    [SerializeField]
    SkinnedMeshRenderer BaseMeshObj;    // ベイクする元のオブジェクト

    [SerializeField]
    GameObject BakeMeshObj;            // ベイクしたメッシュを格納するGameObject

    // BakeMeshObjをインスタンスした際のSkinnedMeshRendererリスト
    List<SkinnedMeshRenderer> BakeCloneMeshList;

    const int CloneCount = 4;       // 残像数
    const int FlameCountMax = 4;    // 残像を更新する頻度
    int FlameCount = 0;

    void Start()
    {
        BakeCloneMeshList = new List<SkinnedMeshRenderer>();

        // 残像を複製
        for (int i = 0; i < CloneCount;i++)
        {
            var obj = Instantiate(BakeMeshObj);
            BakeCloneMeshList.Add(obj.GetComponent<SkinnedMeshRenderer>());
        }
    }

    void FixedUpdate()
    {
        // 4フレームに一度更新
        FlameCount++;
        if (FlameCount % FlameCountMax != 0)
        {
            return;
        }

        // BakeしたMeshを１つ前にずらしていく
        for (int i = BakeCloneMeshList.Count-1; i >= 1; i--)
        {
            BakeCloneMeshList[i].sharedMesh = BakeCloneMeshList[i - 1].sharedMesh;

            // 位置と回転をコピー
            BakeCloneMeshList[i].transform.position = BakeCloneMeshList[i - 1].transform.position;
            BakeCloneMeshList[i].transform.rotation = BakeCloneMeshList[i - 1].transform.rotation;
        }

        // 今のスキンメッシュをBakeする
        // ボトルネックになりやすいから注意！
        Mesh mesh = new Mesh();
        BaseMeshObj.BakeMesh(mesh);
        BakeCloneMeshList[0].sharedMesh = mesh;

        // 位置と回転をコピー
        BakeCloneMeshList[0].transform.position = transform.position;
        BakeCloneMeshList[0].transform.rotation = transform.rotation;
    }
}
