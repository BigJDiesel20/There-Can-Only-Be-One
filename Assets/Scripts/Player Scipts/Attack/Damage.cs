using UnityEngine;

public class Damage
{
    LocalPlayerManager damageDealer;
    public enum AttackDamage { None, Smash, Slash }
    AttackDamage _type;
    float _value;


    public Damage(float value, AttackDamage type)
    {
        this._value = value;
        _type = type;
    }


    public float Value { get => _value; }
    public AttackDamage Type { get => _type; }

    public void Reset()
    {
        _value = 0;
        _type = AttackDamage.None;
    }
}