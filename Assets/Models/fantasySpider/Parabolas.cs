using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Parabolas : MonoBehaviour
{
    [SerializeField] float _parabolaJumpHeight = 2.0f;
    [SerializeField] float _parabolaJumpDuration = 2.0f;
    NavMeshAgent _NPCAgent;
    Vector3 direction;

    public enum OffMeshLinkMoveMethod
    {
        Teleport,
        NormalSpeed,
        Parabola,
        Curve
    }
    public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Parabola;
    public AnimationCurve curve = new AnimationCurve();
    IEnumerator IStart(Vector3 position)
    {
        if (method == OffMeshLinkMoveMethod.NormalSpeed)
            yield return StartCoroutine(NormalSpeed(_NPCAgent, position));
        else if (method == OffMeshLinkMoveMethod.Parabola)
            yield return StartCoroutine(Parabola(_NPCAgent, _parabolaJumpHeight, _parabolaJumpDuration, position));
        else if (method == OffMeshLinkMoveMethod.Curve)
            yield return StartCoroutine(Curve(_NPCAgent, 0.5f, position));
    }

    IEnumerator NormalSpeed(NavMeshAgent _NPCAgent, Vector3 position)
    {
        Vector3 endPos = position + Vector3.up * _NPCAgent.baseOffset;
        while (_NPCAgent.transform.position != endPos)
        {
            _NPCAgent.transform.position = Vector3.MoveTowards(_NPCAgent.transform.position, endPos, _NPCAgent.speed * Time.deltaTime);
            yield return null;
        }
    }
    static bool finishedTranslation = true;
    IEnumerator Parabola(NavMeshAgent _NPCAgent, float height, float duration, Vector3 position)
    {
        finishedTranslation = false;
        float maxDistance = 20.0f;
        Vector3 startPos = transform.position;

        Vector3 randomPoint = Random.insideUnitSphere * maxDistance;
        randomPoint.y = 0;
        NavMeshHit hit;
        Vector3 endPos = (transform.position + direction * maxDistance + randomPoint);
        if (NavMesh.SamplePosition(endPos, out hit, maxDistance, -1))
        {

            float normalizedTime = 0.0f;
            while (normalizedTime < 1.0f)
            {
                float yOffset = height * (normalizedTime - normalizedTime * normalizedTime);

                _NPCAgent.transform.position = Vector3.Lerp(startPos, hit.position, normalizedTime) + yOffset * Vector3.up;

                normalizedTime += Time.deltaTime / duration;

                yield return null;
            }
            finishedTranslation = true;
        }
    }
    IEnumerator Curve(NavMeshAgent _NPCAgent, float duration, Vector3 position)
    {
        finishedTranslation = false;
        float maxDistance = 3.0f;
        Vector3 startPos = transform.position;

        Vector3 randomPoint = Random.insideUnitCircle * maxDistance;
        randomPoint.y = 0;
        NavMeshHit hit;
        Vector3 endPos = (transform.position + direction * maxDistance + randomPoint);
        float normalizedTime = 0.0f;
        if (NavMesh.SamplePosition(endPos, out hit, maxDistance, -1))
        {
            Vector3 p = hit.position;
            while (normalizedTime < 1.0f)
            {
                float yOffset = curve.Evaluate(normalizedTime);
                _NPCAgent.transform.position = Vector3.Lerp(startPos, hit.position, normalizedTime) + Vector3.up * _NPCAgent.baseOffset;
                normalizedTime += Time.deltaTime / duration;
                yield return null;
            }
        }
        finishedTranslation = true;
    }
}
