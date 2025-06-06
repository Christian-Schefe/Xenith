public readonly struct Or<T1, T2>
{
    private readonly T1 val1;
    private readonly T2 val2;
    private readonly bool isT1;

    private Or(T1 val1, T2 val2, bool isT1)
    {
        this.val1 = val1;
        this.val2 = val2;
        this.isT1 = isT1;
    }

    public Or(T1 value) : this(value, default, true) { }
    public Or(T2 value) : this(default, value, false) { }

    public bool IsFormer => isT1;
    public bool IsLatter => !isT1;

    public static Or<T1, T2> Former(T1 value)
    {
        return new Or<T1, T2>(value, default, true);
    }

    public static Or<T1, T2> Latter(T2 value)
    {
        return new Or<T1, T2>(default, value, false);
    }

    public bool TryGet(out T1 value)
    {
        if (isT1)
        {
            value = val1;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGet(out T2 value)
    {
        if (!isT1)
        {
            value = val2;
            return true;
        }
        value = default;
        return false;
    }
}

public readonly struct Nand<T1, T2>
{
    private readonly T1 val1;
    private readonly T2 val2;
    private readonly bool? isT1;

    private Nand(T1 val1, T2 val2, bool? isT1)
    {
        this.val1 = val1;
        this.val2 = val2;
        this.isT1 = isT1;
    }

    public Nand(T1 value) : this(value, default, true) { }
    public Nand(T2 value) : this(default, value, false) { }

    public bool? IsFormer => isT1.HasValue && isT1.Value;
    public bool? IsLatter => isT1.HasValue && !isT1.Value;
    public bool IsNeither => !isT1.HasValue;

    public static Nand<T1, T2> Former(T1 value)
    {
        return new Nand<T1, T2>(value, default, true);
    }

    public static Nand<T1, T2> Latter(T2 value)
    {
        return new Nand<T1, T2>(default, value, false);
    }

    public static Nand<T1, T2> Neither()
    {
        return new Nand<T1, T2>(default, default, null);
    }

    public bool TryGet(out T1 value)
    {
        if (isT1.HasValue && isT1.Value)
        {
            value = val1;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGet(out T2 value)
    {
        if (isT1.HasValue && !isT1.Value)
        {
            value = val2;
            return true;
        }
        value = default;
        return false;
    }
}
