using System.Collections.Generic;
using UnityEngine;

// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviour
{
    private readonly List<Enemy> enemies = new List<Enemy>();

    public float damageMax = 40f;
    public float damageMin = 20f;
    public Enemy enemyPrefab;

    public float healthMax = 200f;
    public float healthMin = 100f;

    public Transform[] spawnPoints;

    public float speedMax = 12f;
    public float speedMin = 3f;

    public Color strongEnemyColor = Color.red;
    private int wave;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameover) return;
        
        if (enemies.Count <= 0) SpawnWave();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        UIManager.Instance.UpdateWaveText(wave, enemies.Count);
    }
    
    private void SpawnWave()
    {
        wave++;

        var spawnCount = Mathf.RoundToInt(wave * 5f); //현재 웨이브 * 5 만큼 좀비를 생성

        for (int i = 0; i < spawnCount; ++i)
        {
            var enemyIntensity = Random.Range(0f, 1f);
            CreateEnemy(enemyIntensity);
        }
    }
    
    /// <summary>
    /// 에너미의 강한 정도를 지정하여 생성한다
    /// </summary>
    /// <param name="intensity"> 0 ~ 1  사이의 값 : 1에 가까울 수록 강한 좀비이다</param>
    private void CreateEnemy(float intensity)
    {
        var health = Mathf.Lerp(healthMin, healthMax, intensity);
        var damage = Mathf.Lerp(damageMin, damageMax, intensity);
        var speed = Mathf.Lerp(speedMin, speedMin, intensity);

        var skinColor = Color.Lerp(Color.white, strongEnemyColor, intensity);

        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        var enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        enemy.Setup(health, damage, speed, speed * 0.3f, skinColor );
        enemies.Add(enemy);

        enemy.OnDeath += () =>
        {
            enemies.Remove(enemy); //사망한 경우 리스트에서 해당 오브젝트를 삭제한다
            Destroy(enemy.gameObject, 10f);
            GameManager.Instance.AddScore(100);
        };
    }
}