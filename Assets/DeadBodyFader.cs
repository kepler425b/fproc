using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadBodyFader : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer _mr;

    IEnumerator FadeOut(float duration)
    {
        float t = 0.0f;
        while(t < 1.0f)
        {
            _mr.material.color = new Color(_mr.material.color.r, _mr.material.color.g, _mr.material.color.b, 1.0f - t);
            t = +Time.time / duration;
            yield return null;
        }
    }

    IEnumerator Destroy(float delay)
    {
        StartCoroutine(FadeOut(delay));
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void Start()
    {
        Transform ws = FindObjectOfType<WorldState>().transform;
        if (ws)
        {
            transform.parent = ws;
        }
        else Debug.LogError("World state transform not found.");

        StartCoroutine(Destroy(5.0f));
    }
}
