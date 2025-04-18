using UnityEngine;
using Yeast;

public interface IBox<T>
{
    public void AddListener(System.Action<T> listener);

    public void RemoveListener(System.Action<T> listener);

    public void ClearListeners();

    public void TriggerChange();
}

public interface IReadBox<T> : IBox<T>
{
    public T Value { get; }
}
public interface IReadWriteBox<T> : IBox<T>
{
    public T Value { get; set; }
}

public abstract class ReadBox<T> : IReadBox<T>
{
    public abstract T Value { get; }
    protected System.Action<T> onChange;

    public virtual void AddListener(System.Action<T> listener)
    {
        onChange += listener;
    }

    public virtual void AddAndCallListener(System.Action<T> listener)
    {
        AddListener(listener);
        listener(Value);
    }

    public virtual void RemoveListener(System.Action<T> listener)
    {
        onChange -= listener;
    }

    public virtual void ClearListeners()
    {
        onChange = null;
    }

    public virtual void TriggerChange()
    {
        onChange?.Invoke(Value);
    }
}

public abstract class Box<T> : IReadBox<T>, IReadWriteBox<T>
{
    public abstract T Value { get; set; }
    protected System.Action<T> onChange;

    public virtual void AddListener(System.Action<T> listener)
    {
        onChange += listener;
    }

    public virtual void AddAndCallListener(System.Action<T> listener)
    {
        AddListener(listener);
        listener(Value);
    }

    public virtual void RemoveListener(System.Action<T> listener)
    {
        onChange -= listener;
    }

    public virtual void ClearListeners()
    {
        onChange = null;
    }

    public virtual void TriggerChange()
    {
        onChange?.Invoke(Value);
    }
}

public class ConstBox<T> : ReadBox<T>
{
    private readonly T value;

    public override T Value => value;

    public ConstBox(T value)
    {
        this.value = value;
    }
}

public class VarBox<T> : Box<T>
{
    private T value;

    public override T Value
    {
        get => value;
        set
        {
            this.value = value;
            onChange?.Invoke(value);
        }
    }

    public VarBox(T value)
    {
        this.value = value;
    }
}

public class DeferredVarBox<T> : Box<T>
{
    private T value;
    private readonly System.Func<T> firstGet;
    private bool firstGetCalled = false;

    public override T Value
    {
        get
        {
            if (!firstGetCalled)
            {
                value = firstGet();
                firstGetCalled = true;
            }
            return value;
        }
        set
        {
            this.value = value;
            onChange?.Invoke(value);
        }
    }

    public DeferredVarBox(System.Func<T> firstGet)
    {
        value = default;
        this.firstGet = firstGet;
    }

    public static DeferredVarBox<T> IntPlayerPrefs(string key, T defaultValue, System.Func<int, T> fromInt, System.Func<T, int> toInt)
    {
        var box = new DeferredVarBox<T>(() => fromInt(PlayerPrefs.GetInt(key, toInt(defaultValue))));
        box.AddListener((value) => PlayerPrefs.SetInt(key, toInt(value)));
        return box;
    }
}

public class LinkedProxyBox<T> : Box<T>
{
    private System.Func<T> getAction;
    private System.Action<T> setAction;
    private Box<T> source;

    public override T Value
    {
        get => getAction();
        set => setAction(value);
    }

    public LinkedProxyBox(Box<T> source)
    {
        this.source = source;
        source.AddListener(Link);
        getAction = () => source.Value;
        setAction = (value) => source.Value = value;
    }

    private void Link(T value)
    {
        onChange?.Invoke(value);
    }

    public override void TriggerChange()
    {
        source.TriggerChange();
    }

    public void RemoveLink()
    {
        source.RemoveListener(Link);
        getAction = null;
        setAction = null;
        source = null;
    }
}

public class ProxyBox<T> : Box<T>
{
    private readonly System.Func<T> getAction;
    private readonly System.Action<T> setAction;

    public ProxyBox(System.Func<T> getAction, System.Action<T> setAction)
    {
        this.getAction = getAction;
        this.setAction = setAction;
    }

    public override T Value
    {
        get => getAction();
        set
        {
            setAction(value);
            onChange?.Invoke(value);
        }
    }
}

public class DerivedBox<T, K> : ReadBox<K>
{
    private System.Func<T, K> derive;
    private Box<T> source;

    public override K Value => derive(source.Value);

    public DerivedBox(Box<T> source, System.Func<T, K> derive)
    {
        this.source = source;
        this.derive = derive;
        source.AddListener(Link);
    }

    private void Link(T value)
    {
        onChange?.Invoke(derive(value));
    }

    public override void TriggerChange()
    {
        source.TriggerChange();
    }

    public void RemoveLink()
    {
        source.RemoveListener(Link);
        derive = null;
        source = null;
    }
}

public class DerivedBox<T1, T2, K> : ReadBox<K>
{
    private System.Func<T1, T2, K> derive;
    private Box<T1> source1;
    private Box<T2> source2;

    public override K Value => derive(source1.Value, source2.Value);

    public DerivedBox(Box<T1> source1, Box<T2> source2, System.Func<T1, T2, K> derive)
    {
        this.source1 = source1;
        this.source2 = source2;
        this.derive = derive;
        source1.AddListener(Link1);
        source2.AddListener(Link2);
    }

    private void Link1(T1 value)
    {
        onChange?.Invoke(derive(value, source2.Value));
    }

    private void Link2(T2 value)
    {
        onChange?.Invoke(derive(source1.Value, value));
    }

    public override void TriggerChange()
    {
        source1.TriggerChange();
        source2.TriggerChange();
    }

    public void RemoveLink()
    {
        source1.RemoveListener(Link1);
        source2.RemoveListener(Link2);
        derive = null;
        source1 = null;
        source2 = null;
    }
}

public interface IPersistentBox
{
    public string Serialize();
}

public class PersistentBox<T> : Box<T>, IPersistentBox
{
    private T value;
    private readonly string key;
    public string Key => key;
    private readonly System.Func<T> defaultFactory;
    private bool isAttached = false;

    public override T Value
    {
        get => Get();
        set
        {
            this.value = value;
            onChange?.Invoke(value);
        }
    }

    private T Get()
    {
        if (!isAttached)
        {
            isAttached = true;
            var saveSystem = Globals<SaveSystem>.Instance;
            saveSystem.AttachBox(key, this);
            if (saveSystem.TryGetValue(key, out var serializedValue))
            {
                if (serializedValue.TryFromJson<T>(out var deserializedValue))
                {
                    value = deserializedValue;
                }
                else
                {
                    Debug.LogWarning($"Failed to deserialize value for key '{key}'. Using default value.");
                    value = defaultFactory();
                }
            }
            else
            {
                value = defaultFactory();
            }
        }
        return value;
    }

    public string Serialize()
    {
        return value.ToJson();
    }

    public PersistentBox(string key, T defaultValue)
    {
        this.key = key;
        defaultFactory = () => defaultValue;
    }

    public PersistentBox(string key, System.Func<T> defaultFactory)
    {
        this.key = key;
        this.defaultFactory = defaultFactory;
    }

    public void Detach()
    {
        if (isAttached)
        {
            isAttached = false;
            var saveSystem = Globals<SaveSystem>.Instance;
            saveSystem.DetachBox(key);
        }
    }
}
