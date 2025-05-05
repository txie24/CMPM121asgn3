using System.Collections.Generic;

public class StatBlock
{
    public List<ValueMod> damage = new();
    public List<ValueMod> mana   = new();
    public List<ValueMod> speed  = new();
    public List<ValueMod> cd     = new();

    public static float Apply(float baseVal, List<ValueMod> mods)
    {
        foreach (var m in mods)
            baseVal = m.op == ModOp.Add ? baseVal + m.v : baseVal * m.v;
        return baseVal;
    }
}
