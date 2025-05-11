using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class Stat
{
    [SerializeField]
    float _value;
    [SerializeField]
    float _min;
    [SerializeField]
    float _max;

    bool _isInitialized;

    bool isDebuffed;
    float _debuffValue;

    MonoBehaviour monoBehaviour;

    StatEvents statEvents;

    public void Initialize(float value, float min, float max, MonoBehaviour monoBehaviour, StatEvents statEvents)
    {
        this._value = value;
        this._min = min;
        this._max = max;

        this.monoBehaviour = monoBehaviour;
        this.statEvents = statEvents;

        statEvents.OnValueChange?.Invoke(value);
        statEvents.OnMinimumChange?.Invoke(min);
        statEvents.OnMaximumChange?.Invoke(max);
        statEvents.OnPercentageChange?.Invoke(value/max);

        _isInitialized = true;
    }

    public void Deactivate()
    {
        this._value = 0;
        this._min = 0;
        this._max = 0;
        _debuffValue = 0;
  monoBehaviour = null;
        statEvents = null;

        _isInitialized = false;
    }

    public float Value { get => (!isDebuffed)?_value:_debuffValue; }
    public float Min { get => _min; }
    public float Max { get => _max; }
    public float Percentage { get => _value / _max; }
    public bool IsInitialized { get => _isInitialized;}

    public void Add(float value)
    {
        this._value += Mathf.Abs(value);
        this._value = Mathf.Clamp(this._value, _min, _max);
        statEvents.OnValueChange?.Invoke(this._value);
        statEvents.OnPercentageChange?.Invoke(this._value / _max);
        if (this._value >= _max)
        {
            statEvents.OnValueMaximum?.Invoke();
        }

        
    }
    public void Subtract(float value)
    {
        this._value -= Mathf.Abs(value);
        this._value = Mathf.Clamp(this._value, _min, _max);
        statEvents.OnValueChange(this._value);
        statEvents.OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            statEvents.OnValueZero?.Invoke();
        }

        if (this._value <= _min)
        {
            statEvents.OnValueMinimum?.Invoke();
        }
    }
    public void SetValue(float value)
    {
        this._value = Mathf.Abs(value);
        this._value = Mathf.Clamp(this._value, _min, _max);
        statEvents.OnValueChange?.Invoke(this._value);
        statEvents.OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            statEvents.OnValueZero?.Invoke();
        }
        if (this._value <= _min)
        {
            statEvents.OnValueMinimum?.Invoke();
        }
        if (this._value >= _max)
        {
            statEvents.OnValueMaximum?.Invoke();
        }
    }

    public void AdjustMinimum(float value)
    {
        this._min = Mathf.Clamp(Mathf.Abs(value),0,_max);
        this._value = Mathf.Clamp(this._value, _min, _max);
        statEvents.OnMinimumChange?.Invoke(this._min);
        statEvents.OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            statEvents.OnValueZero?.Invoke();
        }
    }

    public void AdjustMaximum(float value)
    {
        this._max = Mathf.Clamp(Mathf.Abs(value), _min, Mathf.Infinity);
        this._value = Mathf.Clamp(this._value, _min, _max);
        statEvents.OnMaximumChange?.Invoke(this._max);
        statEvents.OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            statEvents.OnValueZero?.Invoke();
        }
       
        if (this._value <= _min)
        {
            statEvents.OnValueMinimum?.Invoke();
        }
        if (this._value >= _max)
        {
            statEvents.OnValueMaximum?.Invoke();
        }
    }

    public void SetDebuff(float debuffValue,float debuffLength)
    {
        _debuffValue = Mathf.Abs(debuffValue);
        isDebuffed = true;
        statEvents.OnValueChange(_debuffValue);
        statEvents.OnPercentageChange?.Invoke(_value / _max);
        monoBehaviour.StartCoroutine(Timer(debuffLength));
    }

    IEnumerator Timer(float debuffLength)
    {
        yield return new WaitForSeconds(debuffLength);
        isDebuffed = false;
        _debuffValue = _value;
        statEvents.OnValueChange(_value);
    }
}
