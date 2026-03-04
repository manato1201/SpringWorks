using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Debris : MonoBehaviour
{
    const int POINT_MAX = 4096;

    private Vector3[] vertices_;
    private int[] indices_;
    private Color[] colors_;
    private Vector2[] uvs_;
    private float range_;
    private float rangeR_;
    private float move_ = 0f;
    private Matrix4x4 prev_view_matrix_;

    private Camera cam_;
    private MeshRenderer mr_;
    private MaterialPropertyBlock mpb_;

    void Start()
    {
        cam_ = Camera.main;              // 本当は inspector で刺すのが安全
        mr_ = GetComponent<MeshRenderer>();
        mpb_ = new MaterialPropertyBlock();

        range_ = 32f;
        rangeR_ = 1.0f / range_;

        vertices_ = new Vector3[POINT_MAX * 3];
        for (int i = 0; i < POINT_MAX; ++i)
        {
            float x = Random.Range(-range_, range_);
            float y = Random.Range(-range_, range_);
            float z = Random.Range(-range_, range_);
            var p = new Vector3(x, y, z);
            vertices_[i * 3 + 0] = p;
            vertices_[i * 3 + 1] = p;
            vertices_[i * 3 + 2] = p;
        }

        indices_ = new int[POINT_MAX * 3];
        for (int i = 0; i < POINT_MAX * 3; ++i) indices_[i] = i;

        colors_ = new Color[POINT_MAX * 3];
        for (int i = 0; i < POINT_MAX; ++i)
        {
            colors_[i * 3 + 0] = new Color(1f, 1f, 1f, 0f);
            colors_[i * 3 + 1] = new Color(1f, 1f, 1f, 1f);
            colors_[i * 3 + 2] = new Color(1f, 1f, 1f, 0f);
        }

        uvs_ = new Vector2[POINT_MAX * 3];
        for (int i = 0; i < POINT_MAX; ++i)
        {
            uvs_[i * 3 + 0] = new Vector2(1f, 0f);
            uvs_[i * 3 + 1] = new Vector2(1f, 0f);
            uvs_[i * 3 + 2] = new Vector2(0f, 1f);
        }

        var mesh = new Mesh();
        mesh.name = "debris";
        mesh.vertices = vertices_;
        mesh.colors = colors_;
        mesh.uv = uvs_;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 99999999);

        var mf = GetComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        mf.sharedMesh.SetIndices(indices_, MeshTopology.Lines, 0);

        prev_view_matrix_ = cam_.worldToCameraMatrix;
    }

    void Update()
    {
        var cam = Camera.main;
        if (!cam) return;

        // debrisがカメラの子なら、ターゲットはローカルでOK
        var target_position = new Vector3(0f, 0f, range_);

        var matrix = prev_view_matrix_ * cam.cameraToWorldMatrix; // prevView * invCurView

        var mr = GetComponent<Renderer>();
        const float raindrop_speed = -1f;

        mr.material.SetFloat("_Range", range_);
        mr.material.SetFloat("_RangeR", rangeR_);
        mr.material.SetFloat("_MoveTotal", move_);
        mr.material.SetFloat("_Move", raindrop_speed);
        mr.material.SetVector("_TargetPosition", target_position);
        mr.material.SetMatrix("_PrevInvMatrix", matrix);

        move_ += raindrop_speed;
        move_ = Mathf.Repeat(move_, range_ * 2f);
        prev_view_matrix_ = cam.worldToCameraMatrix;
    }
}
