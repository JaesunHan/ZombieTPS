using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }
    
    private PlayerShooter gunHolder;
    private LineRenderer bulletLineRenderer;
    
    private AudioSource gunAudioPlayer;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    
    public ParticleSystem muzzleFlashEffect;
    public ParticleSystem shellEjectEffect;
    
    public Transform fireTransform;
    public Transform leftHandMount;

    public float damage = 25;
    public float fireDistance = 100f;

    public int ammoRemain = 100;
    public int magAmmo;
    public int magCapacity = 30;

    public float timeBetFire = 0.12f;
    public float reloadTime = 1.8f;
    
    [Range(0f, 10f)] public float maxSpread = 3f;
    [Range(1f, 10f)] public float stability = 1f;
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;
    private float currentSpread;
    private float currentSpreadVelocity;

    private float lastFireTime;

    /// <summary>
    /// 레이 캐스트를 하지 않을 대상의 레이어마스크
    /// </summary>
    private LayerMask excludeTarget;

    private void Awake()
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        //[0] : 총구의 위치 / [1] : 탄환이 닿은 위치
        bulletLineRenderer.positionCount = 2;
        bulletLineRenderer.enabled = false;
    }

    public void Setup(PlayerShooter gunHolder)
    {
        this.gunHolder = gunHolder;
        excludeTarget = gunHolder.excludeTarget;
    }

    private void OnEnable()
    {
        //탄환을 풀로 채우기
        magAmmo = magCapacity;
        currentSpread = 0f;
        lastFireTime = 0f;
        state = State.Ready;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public bool Fire(Vector3 aimTarget)
    {
        if (state == State.Ready && Time.time > lastFireTime + timeBetFire)
        {
            var fireDirection = aimTarget - fireTransform.position;

            var xError = Utility.GetRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GetRandomNormalDistribution(0f, currentSpread);

            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;

            currentSpread += 1f / stability;

            lastFireTime = Time.time;
            Shot(fireTransform.position, fireDirection);

            return true;
        }


        return false;
    }
    
    /// <summary>
    /// 실체 총알 발사 처리가 이루어지는 함수
    /// </summary>
    /// <param name="startPoint">총알이 발사되는 지점</param>
    /// <param name="direction">총알이 날아가는 방향</param>
    private void Shot(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;
        Vector3 hitPosition;
        //excludeTarget 에 지정된 레이어들을 제외하고, 레이캐스트를 실행한다.
        if (Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget)) // ~ 비트 연산자는 0 ->1 , 1 -> 0 으로 전환하는 비트 연산자이다
        {
            var target = hit.collider.GetComponent<IDamageable>();
            if (null != target)
            {
                DamageMessage damageMessage;
                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            else
            {
                Debug.Log($"hit Point : {hit.point} / hit.normal : {hit.normal} / hit transform : {hit.transform}");
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }
            hitPosition = hit.point;
        }
        else
        {
            hitPosition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        magAmmo--;
        if (magAmmo <= 0) state = State.Empty;
    }

    /// <summary>
    /// 총알이 맞은 지점에 효과를 출력하는 코루틴 함수
    /// </summary>
    /// <param name="hitPosition"></param>
    /// <returns></returns>
    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        //오디오 소스에서 동시에 여러개를 재생하고자 할 때 PlayOneShot 함수를 호출한다 (총알 연사할 때 주로 사용한다.)
        gunAudioPlayer.PlayOneShot(shotClip);

        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);
        yield return new WaitForSeconds(0.04f) ;

        bulletLineRenderer.enabled = false;
    }

    /// <summary>
    /// 재장전 함수 : 외부에서 실행하는 함수이다
    /// </summary>
    /// <returns></returns>
    public bool Reload()
    {
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(nameof(ReloadRoutine));

        return true;
    }

    private IEnumerator ReloadRoutine()
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);
        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        state = State.Ready;
    }

    /// <summary>
    /// 총알 반동값을 상태에 따라 갱신하는 코드
    /// </summary>
    private void Update()
    {
        currentSpread = Mathf.Clamp(currentSpread, 0, maxSpread);
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);
    }
}