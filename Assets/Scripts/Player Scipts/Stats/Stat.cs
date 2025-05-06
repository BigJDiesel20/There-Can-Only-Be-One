using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class Stat
{
    float _value;
    float _min;
    float _max;

    bool _isInitialized;

    bool isDebuffed;
    float _debuffValue;

    MonoBehaviour monoBehaviour;

    public UnityAction<float> OnValueChange;
    public UnityAction<float> OnMinimumChange;
    public UnityAction<float> OnMaximumChange;
    public UnityAction<float> OnPercentageChange;

    // Detect Death
    public UnityAction OnValueZero;
    public UnityAction OnValueMinimum;
    public UnityAction OnValueMaximum;

    public void Initialize(float value, float min, float max, MonoBehaviour monoBehaviour)
    {
        this._value = value;
        this._min = min;
        this._max = max;

        this.monoBehaviour = monoBehaviour;

        OnValueChange?.Invoke(value);
        OnMinimumChange?.Invoke(min);
        OnMaximumChange?.Invoke(max);
        OnPercentageChange?.Invoke(value/max);

        _isInitialized = true;
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
        OnValueChange?.Invoke(this._value);
        OnPercentageChange?.Invoke(this._value / _max);
        if (this._value >= _max)
        {
            OnValueMaximum?.Invoke();
        }

        
    }
    public void Subtract(float value)
    {
        this._value -= Mathf.Abs(value);
        this._value = Mathf.Clamp(this._value, _min, _max);
        OnValueChange(this._value);
        OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            OnValueZero?.Invoke();
        }

        if (this._value <= _min)
        {
            OnValueMinimum?.Invoke();
        }
    }
    public void SetValue(float value)
    {
        this._value = Mathf.Abs(value);
        this._value = Mathf.Clamp(this._value, _min, _max);
        OnValueChange?.Invoke(this._value);
        OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            OnValueZero?.Invoke();
        }
        if (this._value <= _min)
        {
            OnValueMinimum?.Invoke();
        }
        if (this._value >= _max)
        {
            OnValueMaximum?.Invoke();
        }
    }

    public void AdjustMinimum(float value)
    {
        this._min = Mathf.Clamp(Mathf.Abs(value),0,_max);
        this._value = Mathf.Clamp(this._value, _min, _max);
        OnMinimumChange?.Invoke(this._min);
        OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            OnValueZero?.Invoke();
        }
    }

    public void AdjustMaximum(float value)
    {
        this._max = Mathf.Clamp(Mathf.Abs(value), _min, Mathf.Infinity);
        this._value = Mathf.Clamp(this._value, _min, _max);
        OnMaximumChange?.Invoke(this._max);
        OnPercentageChange?.Invoke(this._value / _max);
        if (this._value <= 0)
        {
            OnValueZero?.Invoke();
        }
       
        if (this._value <= _min)
        {
            OnValueMinimum?.Invoke();
        }
        if (this._value >= _max)
        {
            OnValueMaximum?.Invoke();
        }
    }

    public void SetDebuff(float debuffValue,float debuffLength)
    {
        _debuffValue = Mathf.Abs(debuffValue);
        isDebuffed = true;
        OnValueChange(_debuffValue);
        OnPercentageChange?.Invoke(_value / _max);
        monoBehaviour.StartCoroutine(Timer(debuffLength));
    }

    IEnumerator Timer(float debuffLength)
    {
        yield return new WaitForSeconds(debuffLength);
        isDebuffed = false;
        _debuffValue = _value;
        OnValueChange(_value);
    }
}
