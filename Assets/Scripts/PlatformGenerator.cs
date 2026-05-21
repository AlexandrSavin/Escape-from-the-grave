using UnityEngine;
using System.Collections.Generic;

public class PlatformGenerator : MonoBehaviour
{
    [Header("Префабы")]
    public GameObject normalPlatformPrefab;
    public GameObject finishPlatformPrefab;

    [Header("Настройки")]
    public float yOffset = 2.0f;            // Вертикальное расстояние
    public float xRandomRange = 5.0f;       // Широкий разброс!
    public float platformWidth = 1.5f;      // Ширина вашего спрайта платформы
    public float minGap = 0.5f;             // Минимальный зазор

    [Header("Количество на одной высоте")]
    public int minPlatformsPerStep = 1;
    public int maxPlatformsPerStep = 3;

    [Header("Высота начала генерации")]
    public float startGenerationHeight = 9f;
    public float preGenerateHeight = 10f;   // Чтобы сразу видно было разброс

    [Header("UI - Текст победы")]
    public GameObject winTextObject;
    public float winHeight = 30f;
    private bool winTextShown = false;

    [Header("Финиш")]
    public float finishHeight = 50f;
    private bool levelFinished = false;

    private float lastSpawnY;
    private Transform player;
    private List<float> lastRowXPositions = new List<float>();
    private List<float> currentRowXPositions = new List<float>();
    private bool hasPreGenerated = false;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("Игрок с тегом Player не найден!");

        lastSpawnY = startGenerationHeight;

        if (winTextObject != null)
            winTextObject.SetActive(false);
    }

    void Update()
    {
        if (player == null || levelFinished) return;

        // АКТИВАЦИЯ ТЕКСТА (С ИСПРАВЛЕНИЕМ)
        if (!winTextShown && player.position.y >= winHeight)
        {
            if (winTextObject != null)
            {
                winTextObject.SetActive(true);

                RectTransform rect = winTextObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                }

                winTextShown = true;
                Debug.Log("Текст активирован!");
            }
        }

        if (lastSpawnY >= finishHeight)
        {
            SpawnFinishPlatform();
            levelFinished = true;
            return;
        }

        if (player.position.y > lastSpawnY)
        {
            if (!hasPreGenerated)
            {
                float targetHeight = player.position.y + preGenerateHeight;
                while (lastSpawnY < targetHeight)
                {
                    GenerateOneRow();
                }
                hasPreGenerated = true;
            }
            else
            {
                GenerateOneRow();
            }
        }
    }

    void GenerateOneRow()
    {
        int platformCount = Random.Range(minPlatformsPerStep, maxPlatformsPerStep + 1);
        float currentHeight = lastSpawnY + yOffset;
        currentRowXPositions.Clear();

        for (int i = 0; i < platformCount; i++)
        {
            float newX = Random.Range(-xRandomRange, xRandomRange);
            float minDistance = platformWidth + minGap;

            // Проверка на "столбик" (прямо под предыдущей)
            if (IsDirectlyUnder(newX, 0.5f))
            {
                newX += minDistance;
            }

            // Проверка на слипание в этом ряду
            if (IsTooCloseToCurrentRow(newX, minDistance))
            {
                int attempts = 0;
                while (attempts < 20)
                {
                    newX = Random.Range(-xRandomRange, xRandomRange);
                    if (!IsTooCloseToCurrentRow(newX, minDistance) && !IsDirectlyUnder(newX, 0.5f))
                        break;
                    attempts++;
                }
            }

            newX = Mathf.Clamp(newX, -xRandomRange, xRandomRange);

            Vector3 spawnPos = new Vector3(newX, currentHeight, 0f);
            Instantiate(normalPlatformPrefab, spawnPos, Quaternion.identity);

            currentRowXPositions.Add(newX);
        }

        lastRowXPositions = new List<float>(currentRowXPositions);
        lastSpawnY = currentHeight;
    }

    bool IsTooCloseToCurrentRow(float x, float minDistance)
    {
        foreach (float existingX in currentRowXPositions)
        {
            if (Mathf.Abs(x - existingX) < minDistance) return true;
        }
        return false;
    }

    bool IsDirectlyUnder(float x, float tolerance)
    {
        foreach (float prevX in lastRowXPositions)
        {
            if (Mathf.Abs(x - prevX) < tolerance) return true;
        }
        return false;
    }

    void SpawnFinishPlatform()
    {
        if (finishPlatformPrefab == null) return;
        Vector3 finishPos = new Vector3(0, finishHeight, 0f);
        Instantiate(finishPlatformPrefab, finishPos, Quaternion.identity);
    }
}