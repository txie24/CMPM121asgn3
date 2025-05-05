public enum ModOp { Add, Mul }

public readonly struct ValueMod
{
    public readonly ModOp op;
    public readonly float v;
    public ValueMod(ModOp op, float v) { this.op = op; this.v = v; }
}
