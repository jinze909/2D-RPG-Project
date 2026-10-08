using System;
using System.Reflection;
using UnityEngine;

namespace ResourceChecks
{
    internal static class ResourceBehaviorChecks
    {
        private static int passed, failed;

        private sealed class Fixture
        {
            public readonly PlayerStats Stats = new PlayerStats
            {
                Health = 20f, MaxHealth = 20f, Mana = 12f, MaxMana = 12f
            };
            public readonly PlayerHealth Health = new PlayerHealth();
            public readonly PlayerMana Mana = new PlayerMana();
            public readonly PlayerAnimations Animations = new PlayerAnimations();
            public readonly Animator Animator = new Animator();

            public Fixture()
            {
                SetField(Health, "stats", Stats);
                SetField(Mana, "stats", Stats);
                Animations.Components[typeof(Animator)] = Animator;
                Invoke(Animations, "Awake");
                Health.Components[typeof(PlayerAnimations)] = Animations;
                Invoke(Health, "Awake");
                Input.DamageKeyPressed = false;
            }
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void Invoke(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
        }

        private static void Equal(float expected, float actual, string message)
        {
            if (float.IsNaN(actual) || actual != expected)
                throw new Exception(message + ": expected " + expected + ", actual " + actual);
        }

        private static void Deaths(Fixture fixture, int expected)
        {
            if (fixture.Animator.Triggers.Count != expected)
                throw new Exception("death triggers: expected " + expected + ", actual " + fixture.Animator.Triggers.Count);
            foreach (int trigger in fixture.Animator.Triggers)
                if (trigger != Animator.StringToHash("Dead")) throw new Exception("unexpected animation trigger");
        }

        private static void Check(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
        }

        private static int Main()
        {
            Check("new player session owns full independent runtime stats", delegate
            {
                var template = new PlayerStats { Health = 1f, MaxHealth = 10f, Mana = 0f, MaxMana = 20f };
                var actor = new Player(); SetField(actor, "stats", template); Invoke(actor, "Awake");
                if (ReferenceEquals(actor.Stats, template)) throw new Exception("session mutates the authoring asset");
                Equal(10f, actor.Stats.Health, "new session health"); Equal(20f, actor.Stats.Mana, "new session mana");
                actor.Stats.Health = 0f; Equal(1f, template.Health, "template health preserved");
            });
            Check("two players cannot share current resources", delegate
            {
                var template = new PlayerStats { Health = 5f, MaxHealth = 10f, MaxMana = 20f };
                var a = new Player(); var b = new Player(); SetField(a, "stats", template); SetField(b, "stats", template);
                Invoke(a, "Awake"); Invoke(b, "Awake"); a.Stats.Health = 2f;
                Equal(10f, b.Stats.Health, "other player health");
            });
            Check("health and mana components consume the owner's runtime snapshot", delegate
            {
                var f = new Fixture(); var actor = new Player(); SetField(actor, "stats", f.Stats); Invoke(actor, "Awake");
                f.Health.Components[typeof(Player)] = actor; f.Mana.Components[typeof(Player)] = actor;
                Invoke(f.Health, "Awake"); Invoke(f.Mana, "Awake");
                f.Health.TakeDamage(7f); f.Mana.UseMana(3f);
                Equal(13f, actor.Stats.Health, "runtime health"); Equal(9f, actor.Stats.Mana, "runtime mana");
                Equal(20f, f.Stats.Health, "authoring health"); Equal(12f, f.Stats.Mana, "authoring mana");
            });
            Check("destroying player releases its runtime clone", delegate
            {
                var actor = new Player(); SetField(actor, "stats", new PlayerStats { MaxHealth = 10f }); Invoke(actor, "Awake");
                var snapshot = actor.Stats; Invoke(actor, "OnDestroy");
                if (!snapshot.Destroyed) throw new Exception("runtime snapshot leaked");
            });
            Check("retry restores resources and cancels a pending defeat", delegate
            {
                var f = new Fixture(); var actor = new Player(); SetField(actor, "stats", f.Stats);
                actor.Components[typeof(PlayerAnimations)] = f.Animations; Invoke(actor, "Awake");
                actor.Stats.Health = 0f; actor.Stats.Mana = 0f; f.Animations.SetDeadAnimation();
                actor.ResetForNewRun();
                Equal(20f, actor.Stats.Health, "retry health"); Equal(12f, actor.Stats.Mana, "retry mana");
                if (f.Animator.Triggers.Contains(Animator.StringToHash("Dead"))) throw new Exception("defeat trigger survived retry");
                if (!f.Animator.Triggers.Contains(Animator.StringToHash("Revive"))) throw new Exception("revive trigger missing");
            });
            Check("defeat cancels a pending revive", delegate
            {
                var f = new Fixture(); f.Animations.SetReviveAnimation(); f.Animations.SetDeadAnimation();
                if (f.Animator.Triggers.Contains(Animator.StringToHash("Revive"))) throw new Exception("revive survived later defeat");
                Deaths(f, 1);
            });
            Check("invalid resource maxima reset to a bounded empty state", delegate
            {
                var stats = new PlayerStats { MaxHealth = float.NaN, MaxMana = float.PositiveInfinity };
                stats.ResetPlayer(); Equal(0f, stats.Health, "health"); Equal(0f, stats.Mana, "mana");
                stats.MaxHealth = -3f; stats.MaxMana = -2f; stats.ResetPlayer();
                Equal(0f, stats.Health, "negative maximum health"); Equal(0f, stats.Mana, "negative maximum mana");
            });
            Check("mana spend reports accepted and rejected transactions", delegate
            {
                var f = new Fixture();
                if (!f.Mana.TryUseMana(5f)) throw new Exception("valid spend rejected");
                if (f.Mana.TryUseMana(8f)) throw new Exception("insufficient spend accepted");
                Equal(7f, f.Stats.Mana, "mana after rejected spend");
            });
            Check("missing stats reject resource operations safely", delegate
            {
                var health = new PlayerHealth(); var mana = new PlayerMana();
                Invoke(health, "Awake"); Invoke(mana, "Awake"); health.TakeDamage(1f);
                if (mana.TryUseMana(1f)) throw new Exception("missing resource allowed spend");
            });
            Check("normal damage reduces health without death", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(7f);
                Equal(13f, f.Stats.Health, "health"); Deaths(f, 0);
            });
            Check("exact lethal damage triggers death once", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(20f);
                Equal(0f, f.Stats.Health, "health"); Deaths(f, 1);
            });
            Check("overkill clamps health to zero", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(200f);
                Equal(0f, f.Stats.Health, "health"); Deaths(f, 1);
            });
            Check("subsequent hits cannot damage dead player or replay death", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(20f); f.Health.TakeDamage(1f); f.Health.TakeDamage(3f);
                Deaths(f, 1); Equal(0f, f.Stats.Health, "health");
            });
            Check("already dead player receives no new death trigger", delegate
            {
                var f = new Fixture(); f.Stats.Health = 0f; f.Health.TakeDamage(1f);
                Deaths(f, 0); Equal(0f, f.Stats.Health, "health");
            });
            Check("revived player can die again after explicit stats reset", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(20f); f.Stats.ResetPlayer(); f.Health.TakeDamage(20f);
                Equal(0f, f.Stats.Health, "health"); Deaths(f, 2);
            });
            Check("largest finite damage is accepted and bounded", delegate
            {
                var f = new Fixture(); f.Health.TakeDamage(float.MaxValue);
                Equal(0f, f.Stats.Health, "health"); Deaths(f, 1);
            });
            Check("P debug damage input remains functional", delegate
            {
                var f = new Fixture(); Input.DamageKeyPressed = true; Invoke(f.Health, "Update");
                Input.DamageKeyPressed = false;
                Equal(19f, f.Stats.Health, "health"); Deaths(f, 0);
            });
            Check("gameplay can disable the prototype debug damage key", delegate
            {
                var f = new Fixture(); f.Health.DebugDamageEnabled = false;
                Input.DamageKeyPressed = true; Invoke(f.Health, "Update"); Input.DamageKeyPressed = false;
                Equal(20f, f.Stats.Health, "health"); Deaths(f, 0);
            });
            float[] invalid = { 0f, -0f, -1f, float.NaN, float.PositiveInfinity,
                                float.NegativeInfinity, float.MinValue, -float.Epsilon };
            string[] names = { "zero", "negative zero", "negative", "NaN", "positive infinity",
                               "negative infinity", "minimum finite", "negative subnormal" };
            for (int index = 0; index < invalid.Length; index++)
            {
                float amount = invalid[index]; string name = names[index];
                Check("invalid damage rejected: " + name, delegate
                {
                    var f = new Fixture(); f.Health.TakeDamage(amount);
                    Equal(20f, f.Stats.Health, "health"); Deaths(f, 0);
                });
                Check("invalid mana cost rejected: " + name, delegate
                {
                    var f = new Fixture(); f.Mana.UseMana(amount);
                    Equal(12f, f.Stats.Mana, "mana");
                });
            }
            Check("normal mana use deducts exact cost", delegate
            {
                var f = new Fixture(); f.Mana.UseMana(3f); Equal(9f, f.Stats.Mana, "mana");
            });
            Check("exact mana cost leaves zero", delegate
            {
                var f = new Fixture(); f.Mana.UseMana(12f); Equal(0f, f.Stats.Mana, "mana");
            });
            Check("insufficient mana leaves balance unchanged", delegate
            {
                var f = new Fixture(); f.Mana.UseMana(13f); Equal(12f, f.Stats.Mana, "mana");
            });
            Check("zero mana cannot be overspent", delegate
            {
                var f = new Fixture(); f.Stats.Mana = 0f; f.Mana.UseMana(1f); Equal(0f, f.Stats.Mana, "mana");
            });
            Check("repeated valid costs do not overspend", delegate
            {
                var f = new Fixture(); f.Mana.UseMana(5f); f.Mana.UseMana(5f); f.Mana.UseMana(5f);
                Equal(2f, f.Stats.Mana, "mana");
            });
            Check("largest finite mana balance can be consumed", delegate
            {
                var f = new Fixture(); f.Stats.Mana = float.MaxValue; f.Mana.UseMana(float.MaxValue);
                Equal(0f, f.Stats.Mana, "mana");
            });
            Console.WriteLine("RESOURCE_CHECK_RESULT passed=" + passed + " failed=" + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
