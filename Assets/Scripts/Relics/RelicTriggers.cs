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
    public static IRelicTrigger Create(TriggerData d, Relic r) => d.type switch
    {
        "take-damage" => new DamageTrigger(r),
        "deal-damage" => new DamageDealtTrigger(r),
        "on-kill" => new KillTrigger(r),
        "end-wave" => new EndWaveTrigger(r),
        "stand-still" => new StandStillTrigger(r, float.Parse(d.amount)),
        "move-distance" => new MoveDistanceTrigger(r, float.Parse(d.amount)),
        "no-damage" => new NoDamageTrigger(r, float.Parse(d.amount)),
        _ => throw new Exception($"Unknown trigger type: {d.type}")
    };

    // ─── Your existing take-damage trigger ─────────────────────────
    class DamageTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public DamageTrigger(Relic r) { relic = r; }

        void HandleDamage(Vector3 pos, Damage dmg, Hittable target)
        {
            if (target.team == Hittable.Team.PLAYER)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” triggered on PLAYER damage");
                relic.Fire();
            }
        }

        public void Subscribe()
        {
            EventBus.Instance.OnDamage += HandleDamage;
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to OnDamage");
        }

        public void Unsubscribe()
        {
            EventBus.Instance.OnDamage -= HandleDamage;
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from OnDamage");
        }
    }

    // ─── 1) deal-damage ────────────────────────────────────────────
    class DamageDealtTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public DamageDealtTrigger(Relic r) { relic = r; }

        void HandleDamage(Vector3 pos, Damage dmg, Hittable target)
        {
            if (target.team == Hittable.Team.MONSTERS)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” triggered on damage dealt");
                relic.Fire();
            }
        }

        public void Subscribe()
        {
            EventBus.Instance.OnDamage += HandleDamage;
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to OnDamage (deal-damage)");
        }

        public void Unsubscribe()
        {
            EventBus.Instance.OnDamage -= HandleDamage;
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from OnDamage (deal-damage)");
        }
    }

    // ─── 2) on-kill ────────────────────────────────────────────────
    class KillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public KillTrigger(Relic r) { relic = r; }

        void OnKilled(GameObject enemy)
        {
            Debug.Log($"[RelicTrigger] “{relic.Name}” triggered on enemy kill");
            relic.Fire();
        }

        public void Subscribe()
        {
            EnemySpawner.OnEnemyKilled += OnKilled;
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to OnEnemyKilled");
        }

        public void Unsubscribe()
        {
            EnemySpawner.OnEnemyKilled -= OnKilled;
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from OnEnemyKilled");
        }
    }

    // ─── 3) end-wave ───────────────────────────────────────────────
    class EndWaveTrigger : IRelicTrigger
    {
        readonly Relic relic;
        public EndWaveTrigger(Relic r) { relic = r; }

        public void Subscribe()
        {
            EnemySpawner.OnWaveEnd += OnWaveEnd;
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to OnWaveEnd");
        }

        public void Unsubscribe()
        {
            EnemySpawner.OnWaveEnd -= OnWaveEnd;
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from OnWaveEnd");
        }

        void OnWaveEnd(int wave)
        {
            // instead of firing immediately, wait one frame so class‐scaling runs first
            CoroutineManager.Instance.StartCoroutine(DelayedFire());
        }

        IEnumerator DelayedFire()
        {
            yield return null;  // wait one frame
            Debug.Log($"[RelicTrigger] “{relic.Name}” firing (delayed) on wave end");
            relic.Fire();
        }
    }

    // ─── 4) stand-still (Jade Elephant) ───────────────────────────
    class StandStillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float secs;
        bool buffActive = false;
        Coroutine watcher = null;

        public StandStillTrigger(Relic r, float amount)
        {
            relic = r;
            secs = amount;
        }

        public void Subscribe()
        {
            PlayerController.OnPlayerMove += HandleMove;
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            if (pc.unit.movement.sqrMagnitude < 0.01f)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” initial stand-still, starting timer");
                StartWatcher(pc);
            }
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to stand-still for {secs}s");
        }

        public void Unsubscribe()
        {
            PlayerController.OnPlayerMove -= HandleMove;
            StopWatcher();
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from stand-still");
        }

        void HandleMove(Vector3 v)
        {
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            if (v.sqrMagnitude < 0.01f)
            {
                if (!buffActive && watcher == null)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” stopped moving, starting timer");
                    StartWatcher(pc);
                }
            }
            else
            {
                if (watcher != null)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” movement, cancel timer");
                    StopWatcher();
                }
                if (buffActive)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” movement, removing buff");
                    relic.End();
                    buffActive = false;
                }
            }
        }

        void StartWatcher(PlayerController pc)
        {
            watcher = CoroutineManager.Instance.StartCoroutine(CheckStandStill(pc));
        }

        void StopWatcher()
        {
            if (watcher != null)
            {
                CoroutineManager.Instance.StopCoroutine(watcher);
                watcher = null;
            }
        }

        IEnumerator CheckStandStill(PlayerController pc)
        {
            yield return new WaitForSeconds(secs);
            if (pc.unit.movement.sqrMagnitude < 0.01f && !buffActive)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” stand-still met");
                relic.Fire();
                buffActive = true;
            }
            watcher = null;
        }
    }

    // ─── 5) move-distance ──────────────────────────────────────────
    class MoveDistanceTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float distanceNeeded;
        float accumulated = 0f;

        public MoveDistanceTrigger(Relic r, float d)
        {
            relic = r;
            distanceNeeded = d;
        }

        void OnMove(Vector3 delta)
        {
            accumulated += delta.magnitude;
            if (accumulated >= distanceNeeded)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” moved {distanceNeeded}, firing");
                relic.Fire();
                accumulated = 0f;
            }
        }

        public void Subscribe()
        {
            PlayerController.OnPlayerMove += OnMove;
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to move-distance ({distanceNeeded})");
        }

        public void Unsubscribe()
        {
            PlayerController.OnPlayerMove -= OnMove;
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from move-distance");
        }
    }

    // ─── 6) no-damage ──────────────────────────────────────────────
    class NoDamageTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float secs;
        Coroutine watcher;

        public NoDamageTrigger(Relic r, float s)
        {
            relic = r;
            secs = s;
        }

        public void Subscribe()
        {
            EventBus.Instance.OnDamage += ResetTimer;
            StartWatcher();
            Debug.Log($"[RelicTrigger] “{relic.Name}” subscribed to no-damage for {secs}s");
        }

        public void Unsubscribe()
        {
            EventBus.Instance.OnDamage -= ResetTimer;
            if (watcher != null) CoroutineManager.Instance.StopCoroutine(watcher);
            Debug.Log($"[RelicTrigger] “{relic.Name}” unsubscribed from no-damage");
        }

        void ResetTimer(Vector3 _, Damage __, Hittable target)
        {
            if (target.team == Hittable.Team.PLAYER)
            {
                if (watcher != null) CoroutineManager.Instance.StopCoroutine(watcher);
                StartWatcher();
                Debug.Log($"[RelicTrigger] “{relic.Name}” reset no-damage timer");
            }
        }

        void StartWatcher()
        {
            watcher = CoroutineManager.Instance.StartCoroutine(WaitAndFire());
        }

        IEnumerator WaitAndFire()
        {
            yield return new WaitForSeconds(secs);
            Debug.Log($"[RelicTrigger] “{relic.Name}” no-damage met after {secs}s");
            relic.Fire();
            watcher = null;
        }
    }
}
