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
        "on-kill" => new KillTrigger(r),
        "stand-still" => new StandStillTrigger(r, float.Parse(d.amount)),
        _ => throw new Exception($"Unknown trigger type: {d.type}")
    };

    // ─── DamageTrigger (unchanged) ───────────────────────────────────
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

    // ─── KillTrigger (unchanged) ─────────────────────────────────────
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

    // ─── StandStillTrigger (updated) ────────────────────────────────
    class StandStillTrigger : IRelicTrigger
    {
        readonly Relic relic;
        readonly float secs;

        // tracks whether we've already applied the buff
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

            // if you’re already standing still at pickup, kick off the timer immediately
            var pc = GameManager.Instance.player.GetComponent<PlayerController>();
            if (pc.unit.movement.sqrMagnitude < 0.01f)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” initial stand-still detected, starting timer");
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
                // you’ve come to a stop
                if (!buffActive && watcher == null)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” stopped moving, starting stand-still timer");
                    StartWatcher(pc);
                }
            }
            else
            {
                // any movement at all (including diagonals) cancels timer or buff
                if (watcher != null)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” movement detected, cancel stand-still timer");
                    StopWatcher();
                }
                if (buffActive)
                {
                    Debug.Log($"[RelicTrigger] “{relic.Name}” movement detected, removing buff");
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

            // only fire if you’re still truly stopped
            if (pc.unit.movement.sqrMagnitude < 0.01f && !buffActive)
            {
                Debug.Log($"[RelicTrigger] “{relic.Name}” stand-still condition met");
                relic.Fire();
                buffActive = true;
            }

            watcher = null;
        }
    }
}
