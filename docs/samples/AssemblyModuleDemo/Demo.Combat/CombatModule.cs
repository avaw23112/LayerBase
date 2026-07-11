using Demo.Foundation;
using LayerBase.Async;
using LayerBase.DI;
using LayerBase.Layers;
using LayerBase.Modules;
using LayerBase.Scope;

namespace Demo.Combat;

// DTO contracts (defined here so generators can see their attributes)
[ScopeCall<CombatScope, DamageResult>]
public readonly struct CalculateDamageCall
{
    public CalculateDamageCall(int attackerId, int skillPower)
    {
        AttackerId = attackerId;
        SkillPower = skillPower;
    }
    public int AttackerId { get; }
    public int SkillPower { get; }
}

[ScopeEvent<CombatScope>]
public readonly struct CombatantDamagedEvent
{
    public CombatantDamagedEvent(int health) { Health = health; }
    public int Health { get; }
}

public readonly struct DamageResult
{
    public DamageResult(int damage, int remainingHealth)
    {
        Damage = damage;
        RemainingHealth = remainingHealth;
    }
    public int Damage { get; }
    public int RemainingHealth { get; }
}

[AssemblyModule]
public sealed partial class CombatModule { }

[Scope<CombatScope>]
public sealed partial class CombatService : IService
{
    private int _health = 100;
    private int _totalHits;

    public int LastHealth => _health;
    public int TotalHits => _totalHits;

    public void ConfigureServices(IServiceCollection services) { }

    [ScopeEvent]
    private void OnCombatantDamaged(CombatantDamagedEvent message)
    {
        _health = message.Health;
        _totalHits++;
    }

    [ScopeCall]
    private async LBTask<DamageResult> OnCalculateDamage(CalculateDamageCall call)
    {
        _totalHits++;
        int damage = call.AttackerId * call.SkillPower / 10;
        int remaining = _health - damage;
        return new DamageResult(damage, remaining);
    }
}

// Layer that hosts the scoped service
public sealed partial class CombatLayer : Layer
{
    public CombatLayer()
    {
        CombatService = new CombatService();
        RegisterService(typeof(CombatService), CombatService);
    }

    public CombatService CombatService { get; }
}
