using System;
using UnityEngine;

public interface IRelicTrigger
{
    void Subscribe();
    void Unsubscribe();
}

public static class RelicTriggers
{
    public static IRelicTrigger Create(TriggerData d, Relic r) => d.type switch
    {
        "take-damage" => new DamageTrigger(r),
        "on-kill" => new KillTrigger(r),
        "stand-still" => new StandStillTrigger(r, float.Parse(d.amount)),
        _ => throw new Exception($"Unknown trigger {d.type}")
    };

    class DamageTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public DamageTrigger(Relic r) { relic = r; }
        public void Subscribe() => EventBus.Instance.OnDamage += (_, __, ___) => relic.Fire();
        public void Unsubscribe() => EventBus.Instance.OnDamage -= (_, __, ___) => relic.Fire();
    }

    class KillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public KillTrigger(Relic r) { relic = r; }
        public void Subscribe() => EnemySpawner.OnEnemyKilled += OnKilled;
        public void Unsubscribe() => EnemySpawner.OnEnemyKilled -= OnKilled;
        void OnKilled(GameObject _) => relic.Fire();
    }

    class StandStillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float secs;
        bool waiting;

        public StandStillTrigger(Relic r, float s) { relic = r; secs = s; }

        public void Subscribe()
        {
            PlayerController.OnPlayerMove += OnMove;
            CoroutineManager.Instance.Run(Check());
        }
        public void Unsubscribe()
            => PlayerController.OnPlayerMove -= OnMove;

        void OnMove(Vector3 mv)
        {
            if (mv.sqrMagnitude <= 0.001f && !waiting)
                CoroutineManager.Instance.Run(Check());
            else if (mv.sqrMagnitude > 0.001f && waiting)
            {
                waiting = false;
                relic.End();
            }
        }

        System.Collections.IEnumerator Check()
        {
            waiting = true;
            yield return new WaitForSeconds(secs);
            if (waiting) relic.Fire();
        }
    }
}
