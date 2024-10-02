using System;

public struct CPUFlags
{
    [Flags]
    public enum Flags : ushort {
        StatusCarry
            = 0b000000001,
        StatusParity
            = 0b000000010,
        StatusAuxCarry
            = 0b000000100,
        StatusZero
            = 0b000001000,
        StatusSign
            = 0b000010000,
        StatusOverflow
            = 0b000100000,
        ControlInterruptEnabled
            = 0b001000000,
        ControlDirection
            = 0b010000000,
        ControlTrap
            = 0b100000000,
    }

    private Flags flags;

    public readonly bool GetFlag(Flags flag) => flags.HasFlag(flag);
    public void SetFlag(Flags flag, bool value) => flags = value ? flags | flag : flags & ~flag;

    public bool StatCarry            { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool StatParity           { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool StatAuxCarry         { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool StatZero             { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool StatSign             { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool StatOverflow         { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool CtrlInterruptEnabled { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool CtrlDirection        { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
    public bool CtrlTrap             { readonly get => GetFlag(Flags.StatusCarry); set => SetFlag(Flags.StatusCarry, value); }
}
