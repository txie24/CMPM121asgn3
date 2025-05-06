using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// 构建随机法术链，通过实例化全局命名空间中的法术类
/// </summary>
public class SpellBuilder
{
    private readonly Dictionary<string, JObject> catalog;
    private readonly System.Random rng = new System.Random();

    public SpellBuilder()
    {
        var ta = Resources.Load<TextAsset>("spells");
        if (ta == null)
        {
            Debug.LogError("SpellBuilder: spells.json not found in Resources!");
            catalog = new Dictionary<string, JObject>();
        }
        else
        {
            catalog = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(ta.text);
            Debug.Log($"SpellBuilder: Loaded {catalog.Count} spell definitions.");
        }
    }

    /// <summary>
    /// 生成一个随机法术，可能带有修饰符
    /// </summary>
    public Spell Build(SpellCaster owner)
    {
        // 选择一个基础法术
        Spell baseSpell = CreateRandomBaseSpell(owner);
        
        // 随机决定是否添加修饰符，以及添加多少个
        int modifierCount = Random.Range(0, 3); // 0-2个修饰符
        
        // 应用修饰符
        Spell result = baseSpell;
        for (int i = 0; i < modifierCount; i++)
        {
            result = ApplyRandomModifier(result);
        }
        
        return result;
    }
    
    private Spell CreateRandomBaseSpell(SpellCaster owner)
    {
        // 随机选择一个基础法术类型
        int spellType = Random.Range(0, 4); // 0-3，对应4种基础法术
        
        switch (spellType)
        {
            case 0: return new ArcaneBolt(owner);
            case 1: return new ArcaneSpray(owner);
            case 2: return new MagicMissile(owner);
            case 3: return new ArcaneExplosion(owner);
            // 如果你添加了自定义基础法术，也在这里添加一个case
            default: return new ArcaneBolt(owner); // 默认使用ArcaneBolt
        }
    }
    
    private Spell ApplyRandomModifier(Spell spell)
    {
        // 随机选择一个修饰符类型
        int modType = Random.Range(0, 6); // 0-5，对应6种修饰符
        
        switch (modType)
        {
            case 0: return new Splitter(spell);         // 注意这里的类名变化
            case 1: return new Doubler(spell);          // 注意这里的类名变化
            case 2: return new DamageMagnifier(spell);  // 注意这里的类名变化
            case 3: return new SpeedModifier(spell);
            case 4: return new ChaoticModifier(spell);
            case 5: return new HomingModifier(spell);
            // 如果你添加了自定义修饰符，也在这里添加case
            default: return spell; // 如果出现问题，返回原始法术
        }
    }
}