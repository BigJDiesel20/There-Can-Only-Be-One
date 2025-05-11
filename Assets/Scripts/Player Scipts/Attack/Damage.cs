using UnityEngine;

public class Damage
{
    LocalPlayerManager damageDealer;
    public enum AttackType { None, Smash, Slash }
    AttackType _type;
    float _value;
    bool _isReset = true;


    public Damage(float value, AttackType type)
    {
        this._value = value;
        _type = type;
        _isReset = false;
    }


    public float Value { get => _value; }
    public AttackType Type { get => _type; }

    public void Reset()
    {
        _value = 0;
        _type = AttackType.None;
        _isReset = true;
    }
}