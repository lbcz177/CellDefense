using UnityEngine;

// Only sprite vertices move; placement, root transforms, muzzle coordinates and colliders remain authoritative.
[DisallowMultipleComponent, DefaultExecutionOrder(60)]
public sealed class TowerImpactRecoil : MonoBehaviour
{
    private static readonly int Kick = Shader.PropertyToID("_ImpactKick"), Squash = Shader.PropertyToID("_ImpactSquash"), Direction = Shader.PropertyToID("_ImpactDirection");
    private SpriteRenderer body;private MaterialPropertyBlock block;private float elapsed=-1f,strength;private Vector3 direction;
    private int startedFrame;
    private void Awake()
    {
        body=GetComponent<SpriteRenderer>();block=new MaterialPropertyBlock();
        CellImpactArtwork art=CellCombatFeedback.Artwork;
        if(body!=null && art!=null && art.spriteMaterial!=null)body.sharedMaterial=art.spriteMaterial;
    }
    public void Play(Vector3 worldDirection, float intensity=1f)
    {
        if(body==null)return;
        direction=transform.InverseTransformDirection(worldDirection).normalized;
        if(direction.sqrMagnitude<.01f)direction=Vector3.right;
        strength=Mathf.Clamp(intensity,.1f,1.2f);elapsed=0f;startedFrame=Time.frameCount;Apply();
    }
    private void LateUpdate()
    {
        if(elapsed<0f||Time.deltaTime<=0f||Time.frameCount==startedFrame)return;
        elapsed+=Time.deltaTime;if(elapsed>=.16f)elapsed=-1f;Apply();
    }
    private void Apply()
    {
        if(body==null)return;
        float phase=elapsed<0f?1f:elapsed/.16f;
        float impulse=elapsed<0f?0f:Mathf.Cos(phase*Mathf.PI*2f)*Mathf.Exp(-phase*4.5f)*strength;
        body.GetPropertyBlock(block);block.SetFloat(Kick,.055f*impulse);block.SetFloat(Squash,.10f*impulse);
        block.SetVector(Direction,direction);body.SetPropertyBlock(block);
    }
    private void OnDisable(){elapsed=-1f;Apply();}
}
