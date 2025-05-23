using System;
using UnityEngine;
using System.Collections;

public interface IRelicTrigger
{
    void Subscribe();
    void Unsubscribe();
}

public static class RelicTriggers
{
    public static IRelicTrigger Create(TriggerData data, Relic relic)
    {
        return data.type switch
        {
            "take-damage" => new TakeDamageTrigger(relic),
            "on-kill" => new OnKillTrigger(relic),
            "stand-still" => new StandStillTrigger(relic, float.Parse(data.amount)),
            _ => throw new Exception($"Unknown trigger type: {data.type}")
        };
    }

    class TakeDamageTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public TakeDamageTrigger(Relic r) { relic = r; }

        public void Subscribe()
        {
            EventBus.Instance.OnDamage += OnDamaged;
        }

        public void Unsubscribe()
        {
            EventBus.Instance.OnDamage -= OnDamaged;
        }

        void OnDamaged(Vector3 pos, float dmg, GameObject source)
        {
            relic.Fire();
        }
    }

    class OnKillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public OnKillTrigger(Relic r) { relic = r; }

        public void Subscribe()
        {
            EnemySpawner.OnEnemyKilled += OnKilled;
        }

        public void Unsubscribe()
        {
            EnemySpawner.OnEnemyKilled -= OnKilled;
        }

        void OnKilled(GameObject killedEnemy)
        {
            relic.Fire();
        }
    }

    class StandStillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float delay;
        bool isWaiting;

        public StandStillTrigger(Relic r, float secs)
        {
            relic = r;
            delay = secs;
        }

        public void Subscribe()
        {
            PlayerController.OnStopped += OnStopped;
            PlayerController.OnMoved += OnMoved;
        }

        public void Unsubscribe()
        {
            PlayerController.OnStopped -= OnStopped;
            PlayerController.OnMoved -= OnMoved;
        }

        void OnStopped()
        {
            if (!isWaiting)
                CoroutineManager.Instance.Run(WaitAndFire());
        }

        IEnumerator WaitAndFire()
        {
            isWaiting = true;
            yield return new WaitForSeconds(delay);
            relic.Fire();
        }

        void OnMoved()
        {
            isWaiting = false;
            relic.End();
        }
    }
}
